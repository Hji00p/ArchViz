using ArchViz.Application.Scenes;

namespace ArchViz.Application.Communication;

public record ChatMessageResponse(int Id, string SenderId, string Content, DateTimeOffset SentAt);
public record NotificationResponse(int Id, string Title, int OrderId, int? SceneId,
    DateTimeOffset CreatedAt, DateTimeOffset? ReadAt);

public interface ICommunicationRepository
{
    Task<IReadOnlyList<ChatMessageResponse>?> ListMessagesAsync(
        int orderId, int? sceneId, string userId, int? beforeId, CancellationToken ct);
    Task<DeliveryResult> SendAsync(
        int orderId, int? sceneId, string userId, string content, CancellationToken ct);
    Task<IReadOnlyList<NotificationResponse>> ListNotificationsAsync(
        string userId, int? beforeId, CancellationToken ct);
    Task<bool> MarkReadAsync(int notificationId, string userId, CancellationToken ct);
}
