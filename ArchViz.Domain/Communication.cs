namespace ArchViz.Domain.Communication;

public sealed class ChatMessage
{
    public int Id { get; private set; }
    public int? OrderId { get; private set; }
    public int? SceneId { get; private set; }
    public string SenderId { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public DateTimeOffset SentAt { get; private set; }

    private ChatMessage() { }

    public ChatMessage(int orderId, int? sceneId, string senderId, string content)
    {
        OrderId = sceneId is null ? orderId : null;
        SceneId = sceneId;
        SenderId = senderId;
        Content = content.Trim();
        SentAt = DateTimeOffset.UtcNow;
    }
}

public sealed class Notification
{
    public int Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public int OrderId { get; private set; }
    public int? SceneId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ReadAt { get; private set; }

    private Notification() { }

    public Notification(string userId, string title, int orderId, int? sceneId)
    {
        UserId = userId;
        Title = title;
        OrderId = orderId;
        SceneId = sceneId;
        CreatedAt = DateTimeOffset.UtcNow;
    }
}
