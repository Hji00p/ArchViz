using ArchViz.Application.Scenes;
using ArchViz.Domain.Orders;
using ArchViz.Domain.Scenes;
using Microsoft.EntityFrameworkCore;

namespace ArchViz.Infrastructure.Persistence;

public sealed class SceneTimeRepository(ArchVizDbContext db) : ISceneTimeRepository
{
    public async Task<TimeEntryResult> SaveAsync(
        int sceneId, int? entryId, string userId, TimeEntryInput input,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await db.Database.BeginTransactionAsync(cancellationToken);

        var orderId = await db.Scenes
            .Where(scene => scene.Id == sceneId)
            .Select(scene => (int?)scene.OrderId)
            .SingleOrDefaultAsync(cancellationToken);

        if (orderId is null)
        {
            return new(TimeEntryStatus.NotFound);
        }

        var order = (await db.Orders
            .FromSqlInterpolated($"""SELECT * FROM "Orders" WHERE "Id" = {orderId.Value} FOR UPDATE""")
            .AsNoTracking()
            .ToListAsync(cancellationToken)).SingleOrDefault();

        var scene = (await db.Scenes
            .FromSqlInterpolated($"""SELECT * FROM "Scenes" WHERE "Id" = {sceneId} FOR UPDATE""")
            .AsNoTracking()
            .ToListAsync(cancellationToken)).SingleOrDefault();

        if (order is null || scene is null || scene.OrderId != order.Id ||
            !await HasActiveAssignmentAsync(sceneId, userId, cancellationToken))
        {
            return new(TimeEntryStatus.NotFound);
        }

        TimeEntry? entry = null;
        if (entryId is int id)
        {
            entry = await db.TimeEntries.SingleOrDefaultAsync(
                item => item.Id == id && item.SceneId == sceneId && item.UserId == userId,
                cancellationToken);

            if (entry is null)
            {
                return new(TimeEntryStatus.NotFound);
            }
        }

        if (order.Status is OrderStatus.Cancelled or OrderStatus.Completed ||
            scene.Status == SceneStatus.Closed ||
            (entry is null && scene.Status != SceneStatus.InProgress))
        {
            return new(TimeEntryStatus.InvalidState);
        }

        var description = input.Description.Trim();
        if (entry is null)
        {
            entry = new TimeEntry(sceneId, userId, input.WorkDate, input.Hours, description);
            db.TimeEntries.Add(entry);
        }
        else if (entry.WorkDate != input.WorkDate || entry.Hours != input.Hours ||
                 entry.Description != description)
        {
            db.TimeEntryChanges.Add(new TimeEntryChange(entry));
            entry.Revise(input.WorkDate, input.Hours, description);
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new(TimeEntryStatus.Success, new TimeEntryResponse(
            entry.Id, entry.UserId, entry.WorkDate, entry.Hours, entry.Description));
    }

    public async Task<IReadOnlyList<TimeEntryResponse>?> GetOwnAsync(
        int sceneId, string userId, CancellationToken cancellationToken)
    {
        if (!await HasActiveAssignmentAsync(sceneId, userId, cancellationToken))
        {
            return null;
        }

        return await ProjectEntries(db.TimeEntries
                .Where(entry => entry.SceneId == sceneId && entry.UserId == userId))
            .ToListAsync(cancellationToken);
    }

    public async Task<SceneTimeSummary?> GetSummaryAsync(
        int orderId, int sceneId, string managerId, CancellationToken cancellationToken)
    {
        var canRead = await db.Scenes.AnyAsync(scene =>
            scene.Id == sceneId && scene.OrderId == orderId &&
            db.Orders.Any(order => order.Id == scene.OrderId && order.ManagerId == managerId),
            cancellationToken);

        if (!canRead)
        {
            return null;
        }

        var approvedHours = await (
            from approval in db.ScenePlanApprovals
            join prediction in db.MLPredictions on approval.PredictionId equals prediction.Id
            join assessment in db.Set<SceneAssessment>()
                on prediction.AssessmentId equals assessment.Id
            join revision in db.SceneRequirementsRevisions
                on assessment.RequirementsRevisionId equals revision.Id
            where revision.SceneId == sceneId
            orderby approval.ApprovedAt descending, approval.Id descending
            select (decimal?)approval.ApprovedHours
        ).FirstOrDefaultAsync(cancellationToken);

        var entries = await ProjectEntries(db.TimeEntries.Where(entry => entry.SceneId == sceneId))
            .ToListAsync(cancellationToken);
        var actualHours = entries.Sum(entry => entry.Hours);

        return new SceneTimeSummary(
            approvedHours, actualHours, approvedHours is decimal plan && actualHours > plan,
            entries);
    }

    private Task<bool> HasActiveAssignmentAsync(
        int sceneId, string userId, CancellationToken cancellationToken)
    {
        return db.SceneAssignments.AnyAsync(assignment =>
            assignment.SceneId == sceneId && assignment.VisualizerId == userId &&
            assignment.EndedAt == null, cancellationToken);
    }

    private static IQueryable<TimeEntryResponse> ProjectEntries(IQueryable<TimeEntry> entries)
    {
        return entries.OrderByDescending(entry => entry.WorkDate)
            .ThenByDescending(entry => entry.Id)
            .Select(entry => new TimeEntryResponse(
                entry.Id, entry.UserId, entry.WorkDate, entry.Hours, entry.Description));
    }
}
