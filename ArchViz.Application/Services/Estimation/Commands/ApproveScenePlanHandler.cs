using ArchViz.Application.Orders;
using ArchViz.Application.Scenes;
using ArchViz.Domain.Estimation;
using ArchViz.Domain.Orders;
using ArchViz.Domain.Scenes;

namespace ArchViz.Application.Estimation;

public sealed class ApproveScenePlanHandler
{
    private readonly IOrderRepository _orders;
    private readonly ISceneRepository _scenes;
    private readonly IEstimationRepository _estimation;

    public ApproveScenePlanHandler(
        IOrderRepository orders,
        ISceneRepository scenes,
        IEstimationRepository estimation)
    {
        _orders = orders;
        _scenes = scenes;
        _estimation = estimation;
    }

    public async Task<ApproveScenePlanResult> HandleAsync(
        int orderId,
        int sceneId,
        string managerId,
        int predictionId,
        decimal approvedHours,
        string? adjustmentReason,
        CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdAsync(orderId, cancellationToken);

        if (order is null || order.ManagerId != managerId)
        {
            return new(ApproveScenePlanStatus.NotFound);
        }

        var scene = await _scenes.GetByIdAsync(sceneId, cancellationToken);

        if (scene is null || scene.OrderId != orderId)
        {
            return new(ApproveScenePlanStatus.NotFound);
        }

        if (order.Status != OrderStatus.InPlanning ||
            scene.Status != SceneStatus.AwaitingPlanApproval)
        {
            return new(ApproveScenePlanStatus.InvalidState);
        }

        var revision = await _scenes.GetLatestRequirementsAsync(
            sceneId, cancellationToken);

        if (revision is null)
        {
            return new(ApproveScenePlanStatus.NotFound);
        }

        var assessment = await _scenes.GetLatestAssessmentAsync(
            revision.Id, cancellationToken);

        var prediction = await _estimation.GetPredictionByIdAsync(
            predictionId, cancellationToken);

        if (assessment is null ||
            prediction is null ||
            prediction.AssessmentId != assessment.Id)
        {
            return new(ApproveScenePlanStatus.NotFound);
        }

        if (approvedHours <= 0)
        {
            return new(ApproveScenePlanStatus.InvalidHours);
        }

        if (approvedHours != prediction.PredictedHours &&
            string.IsNullOrWhiteSpace(adjustmentReason))
        {
            return new(ApproveScenePlanStatus.AdjustmentReasonRequired);
        }

        var approval = new ScenePlanApproval(
            prediction.Id, managerId, approvedHours, adjustmentReason);

        scene.ApprovePlan();

        var approvalId = await _scenes.TryApprovePlanAsync(
            scene, approval, cancellationToken);

        return approvalId is null
            ? new(ApproveScenePlanStatus.InvalidState)
            : new(ApproveScenePlanStatus.Success, approvalId);
    }
}