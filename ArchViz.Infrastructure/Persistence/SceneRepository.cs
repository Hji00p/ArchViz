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

    public async Task<int> AddAsync(
        Scene scene,
        CancellationToken cancellationToken)
    {
        _db.Scenes.Add(scene);
        await _db.SaveChangesAsync(cancellationToken);

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
    CancellationToken cancellationToken)
    {
        _db.SceneAssignments.Add(assignment);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);

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
        _db.SceneRequirementsRevisions.Add(revision);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);

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

        var updatedRows = await _db.Scenes
            .Where(savedScene =>
                savedScene.Id == scene.Id &&
                savedScene.Status == SceneStatus.AwaitingPlanApproval &&
                _db.Orders.Any(order =>
                    order.Id == savedScene.OrderId &&
                    order.ManagerId == approval.ApprovedById &&
                    order.Status == OrderStatus.InPlanning))
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

        return updatedRows == 1;
    }

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