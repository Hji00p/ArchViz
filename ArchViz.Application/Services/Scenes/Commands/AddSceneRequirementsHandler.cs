using ArchViz.Application.Orders;
using ArchViz.Domain.Orders;
using ArchViz.Domain.Scenes;

namespace ArchViz.Application.Scenes;

public sealed class AddSceneRequirementsHandler
{
    private readonly IOrderRepository _orders;
    private readonly ISceneRepository _scenes;

    public AddSceneRequirementsHandler(
        IOrderRepository orders,
        ISceneRepository scenes)
    {
        _orders = orders;
        _scenes = scenes;
    }

    public async Task<AddSceneRequirementsResult> HandleAsync(
        int orderId,
        int sceneId,
        string requirements,
        string managerId,
        CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdAsync(
            orderId, cancellationToken);

        if (order is null || order.ManagerId != managerId)
        {
            return new(AddSceneRequirementsStatus.NotFound);
        }

        var scene = await _scenes.GetByIdAsync(
            sceneId, cancellationToken);

        if (scene is null || scene.OrderId != orderId)
        {
            return new(AddSceneRequirementsStatus.NotFound);
        }

        if (order.Status != OrderStatus.InPlanning ||
            scene.Status != SceneStatus.AwaitingAssessment)
        {
            return new(AddSceneRequirementsStatus.InvalidState);
        }

        var latest = await _scenes.GetLatestRequirementsAsync(
            sceneId, cancellationToken);

        var versionNumber = (latest?.VersionNumber ?? 0) + 1;

        var revision = new SceneRequirementsRevision(
            sceneId,
            versionNumber,
            requirements,
            managerId);

        var revisionId = await _scenes.AddRequirementsRevisionAsync(
            revision, cancellationToken);

        if (revisionId is null)
        {
            return new(AddSceneRequirementsStatus.Conflict);
        }

        return new(
            AddSceneRequirementsStatus.Success,
            revisionId,
            versionNumber);
    }
}