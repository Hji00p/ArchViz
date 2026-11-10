using ArchViz.Application.Scenes;

namespace ArchViz.Application.Orders;

public record OrderActionResponse(
    int Id, int OrderId, string Kind, string Status, string RequestedById,
    string? TargetManagerId, string Reason, DateTimeOffset RequestedAt,
    DateTimeOffset? DecidedAt);

public interface IOrderWorkflowRepository
{
    Task<DeliveryResult> RequestTransferAsync(int orderId, string managerId,
        string targetManagerId, string reason, CancellationToken ct);
    Task<DeliveryResult> RequestCancellationAsync(
        int orderId, string clientId, string reason, CancellationToken ct);
    Task<IReadOnlyList<OrderActionResponse>> ListRequestsAsync(string userId, CancellationToken ct);
    Task<DeliveryResult> DecideAsync(int requestId, string userId, bool accept, CancellationToken ct);
    Task<DeliveryResult> ReassignAsync(int orderId, int sceneId, string managerId,
        string visualizerId, string reason, CancellationToken ct);
}
