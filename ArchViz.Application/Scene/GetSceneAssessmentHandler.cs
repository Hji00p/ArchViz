using ArchViz.Application.Orders;

namespace ArchViz.Application.Scenes;

public sealed class GetSceneAssessmentHandler
{
    private readonly IOrderRepository _orders;
    private readonly ISceneRepository _scenes;
    private readonly ISceneHoursPredictor _predictor;

    public GetSceneAssessmentHandler(
        IOrderRepository orders,
        ISceneRepository scenes,
        ISceneHoursPredictor predictor)
    {
        _orders = orders;
        _scenes = scenes;
        _predictor = predictor;
    }

    public async Task<SceneAssessmentResponse?> HandleAsync(
        int orderId,
        int sceneId,
        string managerId,
        CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdAsync(orderId, cancellationToken);

        if (order is null || order.ManagerId != managerId)
        {
            return null;
        }

        var scene = await _scenes.GetByIdAsync(sceneId, cancellationToken);

        if (scene is null || scene.OrderId != orderId)
        {
            return null;
        }

        var revision = await _scenes.GetLatestRequirementsAsync(
            sceneId, cancellationToken);

        if (revision is null)
        {
            return null;
        }

        var assessment = await _scenes.GetLatestAssessmentAsync(
            revision.Id, cancellationToken);

        if (assessment is null)
        {
            return null;
        }

        var predictedHours = _predictor.PredictHours(assessment);

        return new SceneAssessmentResponse(
            assessment.Id,
            scene.Id,
            revision.Id,
            revision.VersionNumber,
            assessment.ViewsCount,
            assessment.ModelingComplexity,
            assessment.CustomModelingRatio,
            assessment.MaterialsComplexity,
            assessment.LightingComplexity,
            assessment.PostProcessingComplexity,
            predictedHours);
    }
}