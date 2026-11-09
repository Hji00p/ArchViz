using ArchViz.Domain.Estimation;
using ArchViz.Domain.Scenes;

namespace ArchViz.Application.Scenes;

public interface ISceneRepository
{
    Task<int?> AddAsync(
        Scene scene,
        string managerId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Scene>> GetByOrderIdAsync(
    int orderId,
    CancellationToken cancellationToken);

    Task<int?> AddAssignmentAsync(
    SceneAssignment assignment,
    string managerId,
    CancellationToken cancellationToken);

    Task<Scene?> GetByIdAsync(
    int id,
    CancellationToken cancellationToken);

    Task<bool> HasActiveAssignmentAsync(
        int sceneId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Scene>> GetAssignedToAsync(
    string visualizerId,
    CancellationToken cancellationToken);

    Task<int?> AddRequirementsRevisionAsync(
    SceneRequirementsRevision revision,
    CancellationToken cancellationToken);

    Task<SceneRequirementsRevision?> GetLatestRequirementsAsync(
        int sceneId,
        CancellationToken cancellationToken);

    Task<SceneRequirementsRevision?> GetLatestAssignedRequirementsAsync(
    int sceneId,
    string visualizerId,
    CancellationToken cancellationToken);

    Task<int?> TrySubmitAssessmentAsync(
    Scene scene,
    SceneAssessment assessment,
    CancellationToken cancellationToken);

    Task<SceneAssessment?> GetLatestAssessmentAsync(
    int requirementsRevisionId,
    CancellationToken cancellationToken);

    Task<int?> TryApprovePlanAsync(
    Scene scene,
    ScenePlanApproval approval,
    CancellationToken cancellationToken);

    Task<bool> TryStartWorkAsync(
    Scene scene,
    string visualizerId,
    CancellationToken cancellationToken);

    Task<Scene?> GetAssignedSceneAsync(
    int sceneId,
    string visualizerId,
    CancellationToken cancellationToken);
}