namespace ArchViz.Domain.Scenes;

public enum RenderStatus
{
    InternalReview,
    InternalChangesRequested,
    Published,
    ClientChangesRequested,
    Accepted
}

public sealed class RenderVersion
{
    public int Id { get; private set; }
    public int SceneId { get; private set; }
    public int VersionNumber { get; private set; }
    public string UploadedById { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public byte[] Content { get; private set; } = [];
    public RenderStatus Status { get; private set; }
    public DateTimeOffset UploadedAt { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }
    public DateTimeOffset? AcceptedAt { get; private set; }
    public string? PublishedById { get; private set; }
    public string? AcceptedById { get; private set; }

    private RenderVersion() { }

    public RenderVersion(int sceneId, int version, string userId, string fileName, byte[] content)
    {
        SceneId = sceneId;
        VersionNumber = version;
        UploadedById = userId;
        FileName = fileName;
        Content = content;
        UploadedAt = DateTimeOffset.UtcNow;
    }

    public void Publish(string managerId)
    {
        RequireStatus(RenderStatus.InternalReview);
        Status = RenderStatus.Published;
        PublishedById = managerId;
        PublishedAt = DateTimeOffset.UtcNow;
    }

    public void Accept(string clientId)
    {
        RequireStatus(RenderStatus.Published);
        Status = RenderStatus.Accepted;
        AcceptedById = clientId;
        AcceptedAt = DateTimeOffset.UtcNow;
    }

    public void RequestChanges(bool internalReview)
    {
        RequireStatus(internalReview ? RenderStatus.InternalReview : RenderStatus.Published);
        Status = internalReview ? RenderStatus.InternalChangesRequested : RenderStatus.ClientChangesRequested;
    }

    private void RequireStatus(RenderStatus expected)
    {
        if (Status != expected)
        {
            throw new InvalidOperationException("The render is not in the expected review state.");
        }
    }
}
