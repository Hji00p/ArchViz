using ArchViz.Application.Communication;
using ArchViz.Application.Scenes;
using ArchViz.Domain.Communication;
using ArchViz.Domain.Orders;
using Microsoft.EntityFrameworkCore;

namespace ArchViz.Infrastructure.Persistence;

public sealed class CommunicationRepository(ArchVizDbContext db) : ICommunicationRepository
{
    public async Task<IReadOnlyList<ChatMessageResponse>?> ListMessagesAsync(
        int orderId, int? sceneId, string userId, int? beforeId, CancellationToken ct)
    {
        var order = await db.Orders.AsNoTracking().SingleOrDefaultAsync(item => item.Id == orderId, ct);
        if (order is null || !(await ParticipantsAsync(order, sceneId, ct)).Contains(userId))
        {
            return null;
        }

        var query = sceneId is null
            ? db.ChatMessages.Where(message => message.OrderId == orderId)
            : db.ChatMessages.Where(message => message.SceneId == sceneId);

        return await query.Where(message => beforeId == null || message.Id < beforeId)
            .OrderByDescending(message => message.Id).Take(50)
            .Select(message => new ChatMessageResponse(
                message.Id, message.SenderId, message.Content, message.SentAt))
            .ToListAsync(ct);
    }

    public async Task<DeliveryResult> SendAsync(
        int orderId, int? sceneId, string userId, string content, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var orders = await db.Orders
            .FromSqlInterpolated($"""SELECT * FROM "Orders" WHERE "Id" = {orderId} FOR UPDATE""")
            .AsNoTracking().ToListAsync(ct);
        var order = orders.SingleOrDefault();
        if (order is null)
        {
            return new(DeliveryStatus.NotFound);
        }

        var participants = await ParticipantsAsync(order, sceneId, ct);
        if (!participants.Contains(userId))
        {
            return new(DeliveryStatus.NotFound);
        }

        if (order.Status is OrderStatus.Cancelled or OrderStatus.Completed)
        {
            return new(DeliveryStatus.Conflict);
        }

        var message = new ChatMessage(orderId, sceneId, userId, content);
        db.ChatMessages.Add(message);
        db.Notifications.AddRange(participants.Where(id => id != userId)
            .Select(id => new Notification(id, "New message", orderId, sceneId)));

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return new(DeliveryStatus.Success, message.Id);
    }

    public async Task<IReadOnlyList<NotificationResponse>> ListNotificationsAsync(
        string userId, int? beforeId, CancellationToken ct)
    {
        return await db.Notifications
            .Where(item => item.UserId == userId && (beforeId == null || item.Id < beforeId))
            .OrderByDescending(item => item.Id).Take(50)
            .Select(item => new NotificationResponse(item.Id, item.Title, item.OrderId,
                item.SceneId, item.CreatedAt, item.ReadAt))
            .ToListAsync(ct);
    }

    public async Task<bool> MarkReadAsync(int notificationId, string userId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        return await db.Notifications
            .Where(item => item.Id == notificationId && item.UserId == userId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(
                item => item.ReadAt, item => item.ReadAt ?? now), ct) == 1;
    }

    private async Task<HashSet<string>> ParticipantsAsync(Order order, int? sceneId, CancellationToken ct)
    {
        if (sceneId is null)
        {
            return new[] { order.ClientId, order.ManagerId }.OfType<string>().ToHashSet();
        }

        if (!await db.Scenes.AnyAsync(scene => scene.Id == sceneId && scene.OrderId == order.Id, ct))
        {
            return [];
        }

        var ids = await db.SceneAssignments
            .Where(item => item.SceneId == sceneId && item.EndedAt == null)
            .Select(item => item.VisualizerId).ToListAsync(ct);
        if (order.ManagerId is not null)
        {
            ids.Add(order.ManagerId);
        }

        return ids.ToHashSet();
    }
}
