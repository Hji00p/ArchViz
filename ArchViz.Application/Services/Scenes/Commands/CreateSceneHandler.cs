using ArchViz.Application.Orders;
using ArchViz.Domain.Orders;
using ArchViz.Domain.Scenes;

namespace ArchViz.Application.Scenes;

public sealed class CreateSceneHandler
{
    private readonly IOrderRepository _orders;
    private readonly ISceneRepository _scenes;

    public CreateSceneHandler(
        IOrderRepository orders,
        ISceneRepository scenes)
    {
        _orders = orders;
        _scenes = scenes;
    }

    public async Task<CreateSceneResult> HandleAsync(
        int orderId,
        string name,
        string managerId,
        CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdAsync(
            orderId,
            cancellationToken);

        if (order is null || order.ManagerId != managerId)
        {
            return new CreateSceneResult(
                CreateSceneStatus.OrderNotFound);
        }

        if (order.Status != OrderStatus.InPlanning)
        {
            return new CreateSceneResult(
                CreateSceneStatus.OrderNotInPlanning);
        }

        var scene = new Scene(orderId, name);

        var sceneId = await _scenes.AddAsync(
            scene,
            managerId,
            cancellationToken);

        if (sceneId is null)
        {
            return new(CreateSceneStatus.OrderNotInPlanning);
        }

        return new CreateSceneResult(
            CreateSceneStatus.Success,
            sceneId);
    }
}