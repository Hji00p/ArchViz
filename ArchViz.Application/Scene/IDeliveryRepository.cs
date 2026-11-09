using ArchViz.Domain.Scenes;

namespace ArchViz.Application.Scenes;

public enum DeliveryStatus { Success, NotFound, Conflict }
public enum RenderAction { Publish, RequestInternalChanges, Accept, RequestClientChanges }

public record DeliveryResult(DeliveryStatus Status, int? Id = null);
public record DeliveryFile(string Name, byte[] Content);
public record AttachmentResponse(int Id, string FileName, DateTimeOffset UploadedAt);
public record RenderResponse(int Id, int VersionNumber, string FileName,
    string Status, DateTimeOffset UploadedAt, DateTimeOffset? PublishedAt);
public record RevisionResponse(int Id, int RenderVersionId, string Comment,
    bool IsInternal, string? Classification, string? ClassificationReason,
    DateTimeOffset CreatedAt);

public interface IDeliveryRepository
{
    Task<DeliveryResult> AddAttachmentAsync(int orderId, string clientId,
        DeliveryFile file, CancellationToken cancellationToken);
    Task<IReadOnlyList<AttachmentResponse>?> ListAttachmentsAsync(
        int orderId, string userId, CancellationToken cancellationToken);
    Task<DeliveryFile?> ReadAttachmentAsync(int orderId, int id, string userId,
        CancellationToken cancellationToken);
    Task<DeliveryResult> AddRenderAsync(int orderId, int sceneId, string visualizerId,
        DeliveryFile file, CancellationToken cancellationToken);
    Task<IReadOnlyList<RenderResponse>?> ListRendersAsync(int orderId, int sceneId,
        string userId, CancellationToken cancellationToken);
    Task<DeliveryFile?> ReadRenderAsync(int orderId, int sceneId, int renderId,
        string userId, CancellationToken cancellationToken);
    Task<DeliveryResult> ReviewAsync(int orderId, int sceneId, int renderId, string userId,
        RenderAction action, string? comment, CancellationToken cancellationToken);
    Task<IReadOnlyList<RevisionResponse>?> ListRevisionsAsync(int orderId, int sceneId,
        string userId, CancellationToken cancellationToken);
    Task<DeliveryResult> ClassifyAsync(int orderId, int sceneId, int revisionId,
        string managerId, RevisionClassification classification, string reason,
        string? requirements, CancellationToken cancellationToken);
    Task<DeliveryResult> CloseSceneAsync(int orderId, int sceneId, string managerId,
        CancellationToken cancellationToken);
    Task<DeliveryResult> CompleteOrderAsync(int orderId, string managerId,
        CancellationToken cancellationToken);
}
