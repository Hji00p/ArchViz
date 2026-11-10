namespace ArchViz.Domain.Orders;

public sealed class Attachment
{
    public int Id { get; private set; }
    public int OrderId { get; private set; }
    public string UploadedById { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public byte[] Content { get; private set; } = [];
    public DateTimeOffset UploadedAt { get; private set; }

    private Attachment() { }

    public Attachment(int orderId, string userId, string fileName, byte[] content)
    {
        OrderId = orderId;
        UploadedById = userId;
        FileName = fileName;
        Content = content;
        UploadedAt = DateTimeOffset.UtcNow;
    }
}
