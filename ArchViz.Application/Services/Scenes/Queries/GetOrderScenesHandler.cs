using ArchViz.Application.Orders;

namespace ArchViz.Application.Scenes;

public sealed class GetOrderScenesHandler
{
    private readonly IOrderRepository _orders;
    private readonly ISceneRepository _scenes;

    public GetOrderScenesHandler(
        IOrderRepository orders,
        ISceneRepository scenes)
    {
        _orders = orders;
        _scenes = scenes;
    }

    public async Task<IReadOnlyList<SceneResponse>?> HandleAsync(
        int orderId,
        string managerId,
        CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdAsync(
            orderId,
            cancellationToken);

        if (order is null || order.ManagerId != managerId)
        {
            return null;
        }

        var scenes = await _scenes.GetByOrderIdAsync(
            orderId,
            cancellationToken);

        return scenes
            .Select(scene => new SceneResponse(
                scene.Id,
                scene.OrderId,
                scene.Name,
                scene.Status.ToString()))
            .ToList();
    }
}