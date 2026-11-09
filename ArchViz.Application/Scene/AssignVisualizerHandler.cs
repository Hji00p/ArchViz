using ArchViz.Application.Orders;
using ArchViz.Application.Users;
using ArchViz.Domain.Orders;
using ArchViz.Domain.Scenes;

namespace ArchViz.Application.Scenes;

public sealed class AssignVisualizerHandler
{
    private readonly IOrderRepository _orders;
    private readonly ISceneRepository _scenes;
    private readonly IUserDirectory _users;

    public AssignVisualizerHandler(
        IOrderRepository orders,
        ISceneRepository scenes,
        IUserDirectory users)
    {
        _orders = orders;
        _scenes = scenes;
        _users = users;
    }

    public async Task<AssignVisualizerResult> HandleAsync(
        int orderId,
        int sceneId,
        string visualizerId,
        string managerId,
        CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdAsync(
            orderId, cancellationToken);

        if (order is null || order.ManagerId != managerId)
        {
            return new(AssignVisualizerStatus.NotFound);
        }

        var scene = await _scenes.GetByIdAsync(
            sceneId, cancellationToken);

        if (scene is null || scene.OrderId != orderId)
        {
            return new(AssignVisualizerStatus.NotFound);
        }

        if (order.Status != OrderStatus.InPlanning ||
            scene.Status != SceneStatus.AwaitingAssessment)
        {
            return new(AssignVisualizerStatus.InvalidState);
        }

        if (!await _users.IsInRoleAsync(visualizerId, "Visualizer"))
        {
            return new(AssignVisualizerStatus.InvalidVisualizer);
        }

        if (await _scenes.HasActiveAssignmentAsync(
            sceneId, cancellationToken))
        {
            return new(AssignVisualizerStatus.AlreadyAssigned);
        }

        var assignment = new SceneAssignment(sceneId, visualizerId);

        var assignmentId = await _scenes.AddAssignmentAsync(
            assignment, cancellationToken);

        if (assignmentId is null)
        {
            return new(AssignVisualizerStatus.AlreadyAssigned);
        }

        return new(
            AssignVisualizerStatus.Success,
            assignmentId);
    }
}