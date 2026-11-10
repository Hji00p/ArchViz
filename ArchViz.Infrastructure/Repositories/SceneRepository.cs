using ArchViz.Application.Scenes;
using ArchViz.Domain.Scenes;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using ArchViz.Domain.Estimation;
using ArchViz.Domain.Orders;

namespace ArchViz.Infrastructure.Persistence;

public sealed class SceneRepository : ISceneRepository
{
    private readonly ArchVizDbContext _db;

    public SceneRepository(ArchVizDbContext db)
    {
        _db = db;
    }

    public async Task<int?> AddAsync(
        Scene scene,
        string managerId,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await _db.Database.BeginTransactionAsync(cancellationToken);
        var order = await LockOrderAsync(scene.OrderId, cancellationToken);

        if (order is null || order.ManagerId != managerId ||
            order.Status != OrderStatus.InPlanning)
        {
            return null;
        }

        _db.Scenes.Add(scene);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return scene.Id;
    }

    public async Task<IReadOnlyList<Scene>> GetByOrderIdAsync(
    int orderId,
    CancellationToken cancellationToken)
    {
        return await _db.Scenes
            .AsNoTracking()
            .Where(scene => scene.OrderId == orderId)
            .OrderBy(scene => scene.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<int?> AddAssignmentAsync(
        SceneAssignment assignment,
        string managerId,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await _db.Database.BeginTransactionAsync(cancellationToken);
        if (!await CanPlanSceneAsync(assignment.SceneId, managerId, cancellationToken))
        {
            return null;
        }

        _db.SceneAssignments.Add(assignment);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return assignment.Id;
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException postgres &&
                  postgres.SqlState == PostgresErrorCodes.UniqueViolation &&
                  postgres.ConstraintName == "IX_SceneAssignments_SceneId")
        {
            _db.Entry(assignment).State = EntityState.Detached;

            return null;
        }
    }

    public Task<Scene?> GetByIdAsync(
    int id,
    CancellationToken cancellationToken)
    {
        return _db.Scenes
            .AsNoTracking()
            .FirstOrDefaultAsync(
                scene => scene.Id == id,
                cancellationToken);
    }

    public Task<bool> HasActiveAssignmentAsync(
        int sceneId,
        CancellationToken cancellationToken)
    {
        return _db.SceneAssignments
            .AnyAsync(
                assignment =>
                    assignment.SceneId == sceneId &&
                    assignment.EndedAt == null,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Scene>> GetAssignedToAsync(
    string visualizerId,
    CancellationToken cancellationToken)
    {
        return await _db.Scenes
            .AsNoTracking()
            .Where(scene => _db.SceneAssignments.Any(assignment =>
                assignment.SceneId == scene.Id &&
                assignment.VisualizerId == visualizerId &&
                assignment.EndedAt == null))
            .OrderBy(scene => scene.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<int?> AddRequirementsRevisionAsync(
    SceneRequirementsRevision revision,
    CancellationToken cancellationToken)
    {
        await using var transaction =
            await _db.Database.BeginTransactionAsync(cancellationToken);
        if (!await CanPlanSceneAsync(revision.SceneId, revision.CreatedById, cancellationToken))
        {
            return null;
        }

        _db.SceneRequirementsRevisions.Add(revision);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return revision.Id;
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException postgres &&
                  postgres.SqlState == PostgresErrorCodes.UniqueViolation &&
                  postgres.ConstraintName ==
                      "IX_SceneRequirementsRevisions_SceneId_VersionNumber")
        {
            _db.Entry(revision).State = EntityState.Detached;

            return null;
        }
    }

    public Task<SceneRequirementsRevision?> GetLatestRequirementsAsync(
        int sceneId,
        CancellationToken cancellationToken)
    {
        return _db.SceneRequirementsRevisions
            .AsNoTracking()
            .Where(revision => revision.SceneId == sceneId)
            .OrderByDescending(revision => revision.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<SceneRequirementsRevision?> GetLatestAssignedRequirementsAsync(
    int sceneId,
    string visualizerId,
    CancellationToken cancellationToken)
    {
        return _db.SceneRequirementsRevisions
            .AsNoTracking()
            .Where(revision =>
                revision.SceneId == sceneId &&
                _db.SceneAssignments.Any(assignment =>
                    assignment.SceneId == revision.SceneId &&
                    assignment.VisualizerId == visualizerId &&
                    assignment.EndedAt == null))
            .OrderByDescending(revision => revision.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int?> TrySubmitAssessmentAsync(
    Scene scene,
    SceneAssessment assessment,
    CancellationToken cancellationToken)
    {
        await using var transaction =
            await _db.Database.BeginTransactionAsync(cancellationToken);

        var order = await LockOrderAsync(scene.OrderId, cancellationToken);
        if (!IsActive(order))
        {
            return null;
        }

        var updatedRows = await _db.Scenes
            .Where(savedScene =>
                savedScene.Id == scene.Id &&
                savedScene.Status == SceneStatus.AwaitingAssessment &&
                _db.SceneAssignments.Any(assignment =>
                    assignment.SceneId == savedScene.Id &&
                    assignment.VisualizerId == assessment.AssessedById &&
                    assignment.EndedAt == null) &&
                _db.SceneRequirementsRevisions
                    .Where(revision => revision.SceneId == savedScene.Id)
                    .OrderByDescending(revision => revision.VersionNumber)
                    .Select(revision => revision.Id)
                    .FirstOrDefault() == assessment.RequirementsRevisionId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    savedScene => savedScene.Status,
                    scene.Status),
                cancellationToken);

        if (updatedRows == 0)
        {
            return null;
        }

        _db.Set<SceneAssessment>().Add(assessment);
        await _db.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return assessment.Id;
    }

    public Task<SceneAssessment?> GetLatestAssessmentAsync(
    int requirementsRevisionId,
    CancellationToken cancellationToken)
    {
        return _db.Set<SceneAssessment>()
            .AsNoTracking()
            .Where(assessment =>
                assessment.RequirementsRevisionId == requirementsRevisionId)
            .OrderByDescending(assessment => assessment.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int?> TryApprovePlanAsync(
    Scene scene,
    ScenePlanApproval approval,
    CancellationToken cancellationToken)
    {
        await using var transaction =
            await _db.Database.BeginTransactionAsync(cancellationToken);

        var order = await LockOrderAsync(scene.OrderId, cancellationToken);
        if (order is null || order.ManagerId != approval.ApprovedById ||
            order.Status != OrderStatus.InPlanning)
        {
            return null;
        }

        var updatedRows = await _db.Scenes
            .Where(savedScene =>
                savedScene.Id == scene.Id &&
                savedScene.Status == SceneStatus.AwaitingPlanApproval)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    savedScene => savedScene.Status,
                    scene.Status),
                cancellationToken);

        if (updatedRows == 0)
        {
            return null;
        }

        _db.Set<ScenePlanApproval>().Add(approval);
        await _db.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return approval.Id;
    }

    public async Task<bool> TryStartWorkAsync(
    Scene scene,
    string visualizerId,
    CancellationToken cancellationToken)
    {
        await using var transaction =
            await _db.Database.BeginTransactionAsync(cancellationToken);
        var order = await LockOrderAsync(scene.OrderId, cancellationToken);
        if (!IsActive(order))
        {
            return false;
        }

        var updatedRows = await _db.Scenes
            .Where(savedScene =>
                savedScene.Id == scene.Id &&
                savedScene.Status == SceneStatus.Ready &&
                _db.SceneAssignments.Any(assignment =>
                    assignment.SceneId == savedScene.Id &&
                    assignment.VisualizerId == visualizerId &&
                    assignment.EndedAt == null))
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    savedScene => savedScene.Status,
                    scene.Status),
                cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return updatedRows == 1;
    }

    private async Task<bool> CanPlanSceneAsync(
        int sceneId, string managerId, CancellationToken cancellationToken)
    {
        var orderId = await _db.Scenes.Where(scene => scene.Id == sceneId)
            .Select(scene => (int?)scene.OrderId)
            .SingleOrDefaultAsync(cancellationToken);

        if (orderId is null)
        {
            return false;
        }

        var order = await LockOrderAsync(orderId.Value, cancellationToken);
        return order is not null && order.ManagerId == managerId &&
            order.Status == OrderStatus.InPlanning &&
            await _db.Scenes.AnyAsync(
                scene => scene.Id == sceneId && scene.Status == SceneStatus.AwaitingAssessment,
                cancellationToken);
    }

    private async Task<Order?> LockOrderAsync(int orderId, CancellationToken cancellationToken)
    {
        var rows = await _db.Orders
            .FromSqlInterpolated($"""SELECT * FROM "Orders" WHERE "Id" = {orderId} FOR UPDATE""")
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return rows.SingleOrDefault();
    }

    private static bool IsActive(Order? order) =>
        order?.Status is OrderStatus.InPlanning or OrderStatus.InProgress;

    public Task<Scene?> GetAssignedSceneAsync(
    int sceneId,
    string visualizerId,
    CancellationToken cancellationToken)
    {
        return _db.Scenes
            .AsNoTracking()
            .FirstOrDefaultAsync(
                scene => scene.Id == sceneId &&
                    _db.SceneAssignments.Any(assignment =>
                        assignment.SceneId == scene.Id &&
                        assignment.VisualizerId == visualizerId &&
                        assignment.EndedAt == null),
                cancellationToken);
    }
}