using ArchViz.Domain.Scenes;

namespace ArchViz.Application.Scenes;

public sealed class SubmitSceneAssessmentHandler
{
    private readonly ISceneRepository _scenes;

    public SubmitSceneAssessmentHandler(ISceneRepository scenes)
    {
        _scenes = scenes;
    }

    public async Task<SubmitSceneAssessmentResult> HandleAsync(
        int sceneId,
        string visualizerId,
        SubmitSceneAssessmentInput input,
        CancellationToken cancellationToken)
    {
        var revision = await _scenes.GetLatestAssignedRequirementsAsync(
            sceneId, visualizerId, cancellationToken);

        if (revision is null)
        {
            return new(SubmitSceneAssessmentStatus.NotFound);
        }

        if (revision.Id != input.RequirementsRevisionId)
        {
            return new(SubmitSceneAssessmentStatus.Conflict);
        }

        var scene = await _scenes.GetByIdAsync(
            sceneId, cancellationToken);

        if (scene is null)
        {
            return new(SubmitSceneAssessmentStatus.NotFound);
        }

        if (scene.Status != SceneStatus.AwaitingAssessment)
        {
            return new(SubmitSceneAssessmentStatus.Conflict);
        }

        var assessment = new SceneAssessment(
            revision.Id,
            visualizerId,
            input.ViewsCount,
            input.ModelingComplexity,
            input.CustomModelingRatio,
            input.MaterialsComplexity,
            input.LightingComplexity,
            input.PostProcessingComplexity);

        scene.SubmitAssessment();

        var assessmentId = await _scenes.TrySubmitAssessmentAsync(
            scene, assessment, cancellationToken);

        return assessmentId is null
            ? new(SubmitSceneAssessmentStatus.Conflict)
            : new(SubmitSceneAssessmentStatus.Success, assessmentId);
    }
}