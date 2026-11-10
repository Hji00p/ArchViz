using ArchViz.Application.Scenes;
using ArchViz.Domain.Orders;
using ArchViz.Domain.Scenes;
using Microsoft.EntityFrameworkCore;

namespace ArchViz.Infrastructure.Persistence;

public sealed class DeliveryRepository(ArchVizDbContext db) : IDeliveryRepository
{
    public async Task<DeliveryResult> AddAttachmentAsync(
        int orderId, string clientId, DeliveryFile file, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var order = await LockOrderAsync(orderId, ct);
        if (order?.ClientId != clientId)
        {
            return new(DeliveryStatus.NotFound);
        }

        if (!IsOpen(order))
        {
            return new(DeliveryStatus.Conflict);
        }

        var attachment = new Attachment(orderId, clientId, file.Name, file.Content);
        db.Attachments.Add(attachment);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return new(DeliveryStatus.Success, attachment.Id);
    }

    public async Task<IReadOnlyList<AttachmentResponse>?> ListAttachmentsAsync(
        int orderId, string userId, CancellationToken ct)
    {
        if (!await VisibleOrder(orderId, userId).AnyAsync(ct))
        {
            return null;
        }

        return await VisibleAttachments(orderId, userId)
            .OrderByDescending(file => file.Id)
            .Select(file => new AttachmentResponse(file.Id, file.FileName, file.UploadedAt))
            .ToListAsync(ct);
    }

    public Task<DeliveryFile?> ReadAttachmentAsync(
        int orderId, int id, string userId, CancellationToken ct)
    {
        return VisibleAttachments(orderId, userId).Where(file => file.Id == id)
            .Select(file => new DeliveryFile(file.FileName, file.Content))
            .SingleOrDefaultAsync(ct);
    }

    public async Task<DeliveryResult> AddRenderAsync(
        int orderId, int sceneId, string visualizerId, DeliveryFile file, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var order = await LockOrderAsync(orderId, ct);
        var scene = await LockSceneAsync(orderId, sceneId, ct);
        if (order is null || scene is null ||
            !await db.SceneAssignments.AnyAsync(assignment => assignment.SceneId == sceneId &&
                assignment.VisualizerId == visualizerId && assignment.EndedAt == null, ct))
        {
            return new(DeliveryStatus.NotFound);
        }

        var hasPendingRevision = await db.RevisionRequests.AnyAsync(revision =>
            !revision.IsInternal && revision.Classification == null &&
            db.RenderVersions.Any(render => render.Id == revision.RenderVersionId &&
                render.SceneId == sceneId), ct);

        if (!IsOpen(order) || scene.Status != SceneStatus.InProgress || hasPendingRevision)
        {
            return new(DeliveryStatus.Conflict);
        }

        var lastVersion = await Renders(sceneId).MaxAsync(render => (int?)render.VersionNumber, ct);
        var version = new RenderVersion(sceneId, (lastVersion ?? 0) + 1,
            visualizerId, file.Name, file.Content);

        scene.SubmitRender();
        db.RenderVersions.Add(version);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return new(DeliveryStatus.Success, version.Id);
    }

    public async Task<IReadOnlyList<RenderResponse>?> ListRendersAsync(
        int orderId, int sceneId, string userId, CancellationToken ct)
    {
        if (!await VisibleScene(orderId, sceneId, userId).AnyAsync(ct))
        {
            return null;
        }

        var rows = await VisibleRenders(orderId, sceneId, userId)
            .OrderByDescending(render => render.VersionNumber)
            .Select(render => new
            {
                render.Id, render.VersionNumber, render.FileName,
                render.Status, render.UploadedAt, render.PublishedAt
            })
            .ToListAsync(ct);

        return rows.Select(render => new RenderResponse(render.Id, render.VersionNumber,
            render.FileName, render.Status.ToString(), render.UploadedAt, render.PublishedAt))
            .ToArray();
    }

    public Task<DeliveryFile?> ReadRenderAsync(
        int orderId, int sceneId, int renderId, string userId, CancellationToken ct)
    {
        return VisibleRenders(orderId, sceneId, userId).Where(render => render.Id == renderId)
            .Select(render => new DeliveryFile(render.FileName, render.Content))
            .SingleOrDefaultAsync(ct);
    }

    public async Task<DeliveryResult> ReviewAsync(int orderId, int sceneId, int renderId,
        string userId, RenderAction action, string? comment, CancellationToken ct)
    {
        var isManagerAction = action is RenderAction.Publish or RenderAction.RequestInternalChanges;
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var order = await LockOrderAsync(orderId, ct);
        if (order is null || (isManagerAction ? order.ManagerId : order.ClientId) != userId)
        {
            return new(DeliveryStatus.NotFound);
        }

        var scene = await LockSceneAsync(orderId, sceneId, ct);
        var render = await Renders(sceneId).SingleOrDefaultAsync(item => item.Id == renderId, ct);
        if (scene is null || render is null || (!isManagerAction && render.PublishedAt is null))
        {
            return new(DeliveryStatus.NotFound);
        }

        var expectedSceneStatus = isManagerAction ? SceneStatus.InternalReview : SceneStatus.ClientReview;
        var expectedRenderStatus = isManagerAction ? RenderStatus.InternalReview : RenderStatus.Published;
        if (!IsOpen(order) || scene.Status != expectedSceneStatus ||
            render.Status != expectedRenderStatus)
        {
            return new(DeliveryStatus.Conflict);
        }

        RevisionRequest? revision = null;
        switch (action)
        {
            case RenderAction.Publish:
                render.Publish(userId);
                scene.PublishRender();
                break;
            case RenderAction.Accept:
                render.Accept(userId);
                scene.AcceptRender();
                break;
            case RenderAction.RequestInternalChanges:
                render.RequestChanges(internalReview: true);
                scene.ReturnFromInternalReview();
                revision = new RevisionRequest(renderId, userId, comment!, isInternal: true);
                break;
            case RenderAction.RequestClientChanges:
                render.RequestChanges(internalReview: false);
                scene.ReturnFromClientReview();
                revision = new RevisionRequest(renderId, userId, comment!, isInternal: false);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(action));
        }

        if (revision is not null)
        {
            db.RevisionRequests.Add(revision);
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return new(DeliveryStatus.Success, revision?.Id);
    }

    public async Task<IReadOnlyList<RevisionResponse>?> ListRevisionsAsync(
        int orderId, int sceneId, string userId, CancellationToken ct)
    {
        if (!await VisibleScene(orderId, sceneId, userId).AnyAsync(ct))
        {
            return null;
        }

        var rows = await db.RevisionRequests
            .Where(revision => VisibleRenders(orderId, sceneId, userId)
                .Any(render => render.Id == revision.RenderVersionId) &&
                (!revision.IsInternal || InternalSceneAccess(orderId, sceneId, userId).Any()))
            .OrderByDescending(revision => revision.Id)
            .Select(revision => new
            {
                revision.Id, revision.RenderVersionId, revision.Comment,
                revision.IsInternal, revision.Classification,
                revision.ClassificationReason, revision.CreatedAt
            })
            .ToListAsync(ct);

        return rows.Select(revision => new RevisionResponse(revision.Id, revision.RenderVersionId,
            revision.Comment, revision.IsInternal, revision.Classification?.ToString(),
            revision.ClassificationReason, revision.CreatedAt)).ToArray();
    }

    public async Task<DeliveryResult> ClassifyAsync(int orderId, int sceneId, int revisionId,
        string managerId, RevisionClassification classification, string reason,
        string? requirements, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var order = await LockOrderAsync(orderId, ct);
        if (order?.ManagerId != managerId)
        {
            return new(DeliveryStatus.NotFound);
        }

        var scene = await LockSceneAsync(orderId, sceneId, ct);
        var revision = await db.RevisionRequests.SingleOrDefaultAsync(item =>
            item.Id == revisionId && Renders(sceneId).Any(render => render.Id == item.RenderVersionId), ct);
        if (scene is null || revision is null)
        {
            return new(DeliveryStatus.NotFound);
        }

        if (!IsOpen(order) || scene.Status != SceneStatus.InProgress ||
            revision.IsInternal || revision.Classification is not null)
        {
            return new(DeliveryStatus.Conflict);
        }

        SceneRequirementsRevision? requirementsRevision = null;
        if (classification == RevisionClassification.ScopeChange)
        {
            var lastVersion = await db.SceneRequirementsRevisions
                .Where(item => item.SceneId == sceneId)
                .MaxAsync(item => (int?)item.VersionNumber, ct);

            requirementsRevision = new SceneRequirementsRevision(
                sceneId, (lastVersion ?? 0) + 1, requirements!, managerId);
            db.SceneRequirementsRevisions.Add(requirementsRevision);
            scene.ReopenAssessment();
            await db.SaveChangesAsync(ct);
        }

        revision.Classify(classification, managerId, reason, requirementsRevision?.Id);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return new(DeliveryStatus.Success);
    }

    public async Task<DeliveryResult> CloseSceneAsync(
        int orderId, int sceneId, string managerId, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var order = await LockOrderAsync(orderId, ct);
        if (order?.ManagerId != managerId)
        {
            return new(DeliveryStatus.NotFound);
        }

        var scene = await LockSceneAsync(orderId, sceneId, ct);
        if (scene is null)
        {
            return new(DeliveryStatus.NotFound);
        }

        if (!IsOpen(order) || scene.Status != SceneStatus.ClientAccepted ||
            !await Renders(sceneId).AnyAsync(render => render.Status == RenderStatus.Accepted, ct))
        {
            return new(DeliveryStatus.Conflict);
        }

        scene.Close(managerId);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return new(DeliveryStatus.Success);
    }

    public async Task<DeliveryResult> CompleteOrderAsync(
        int orderId, string managerId, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var order = await LockOrderAsync(orderId, ct);
        if (order?.ManagerId != managerId)
        {
            return new(DeliveryStatus.NotFound);
        }

        var scenes = db.Scenes.Where(scene => scene.OrderId == orderId);
        if (order.Status is not (OrderStatus.InPlanning or OrderStatus.InProgress) ||
            !await scenes.AnyAsync(ct) ||
            await scenes.AnyAsync(scene => scene.Status != SceneStatus.Closed, ct))
        {
            return new(DeliveryStatus.Conflict);
        }

        order.Complete();
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return new(DeliveryStatus.Success);
    }

    private IQueryable<RenderVersion> Renders(int sceneId) =>
        db.RenderVersions.Where(render => render.SceneId == sceneId);

    private IQueryable<Order> VisibleOrder(int orderId, string userId) =>
        db.Orders.Where(order => order.Id == orderId &&
            (order.ClientId == userId || order.ManagerId == userId));

    private IQueryable<Attachment> VisibleAttachments(int orderId, string userId) =>
        db.Attachments.Where(file => file.OrderId == orderId &&
            VisibleOrder(orderId, userId).Any());

    private IQueryable<Scene> InternalSceneAccess(int orderId, int sceneId, string userId) =>
        db.Scenes.Where(scene => scene.Id == sceneId && scene.OrderId == orderId &&
            (db.Orders.Any(order => order.Id == scene.OrderId && order.ManagerId == userId) ||
             db.SceneAssignments.Any(assignment => assignment.SceneId == scene.Id &&
                 assignment.VisualizerId == userId && assignment.EndedAt == null)));

    private IQueryable<Scene> VisibleScene(int orderId, int sceneId, string userId) =>
        db.Scenes.Where(scene => scene.Id == sceneId && scene.OrderId == orderId &&
            (InternalSceneAccess(orderId, sceneId, userId).Any() ||
             db.Orders.Any(order => order.Id == scene.OrderId && order.ClientId == userId)));

    private IQueryable<RenderVersion> VisibleRenders(int orderId, int sceneId, string userId) =>
        Renders(sceneId).Where(render =>
            InternalSceneAccess(orderId, sceneId, userId).Any() ||
            (render.PublishedAt != null && VisibleScene(orderId, sceneId, userId).Any()));

    private static bool IsOpen(Order order) =>
        order.Status is not (OrderStatus.Completed or OrderStatus.Cancelled);

    private async Task<Order?> LockOrderAsync(int orderId, CancellationToken ct)
    {
        var rows = await db.Orders
            .FromSqlInterpolated($"""SELECT * FROM "Orders" WHERE "Id" = {orderId} FOR UPDATE""")
            .ToListAsync(ct);
        return rows.SingleOrDefault();
    }

    private async Task<Scene?> LockSceneAsync(int orderId, int sceneId, CancellationToken ct)
    {
        var rows = await db.Scenes
            .FromSqlInterpolated($"""SELECT * FROM "Scenes" WHERE "Id" = {sceneId} AND "OrderId" = {orderId} FOR UPDATE""")
            .ToListAsync(ct);
        return rows.SingleOrDefault();
    }
}
