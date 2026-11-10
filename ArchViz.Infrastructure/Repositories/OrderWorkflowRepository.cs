using ArchViz.Application.Orders;
using ArchViz.Application.Scenes;
using ArchViz.Application.Users;
using ArchViz.Domain.Orders;
using ArchViz.Domain.Scenes;
using Microsoft.EntityFrameworkCore;

namespace ArchViz.Infrastructure.Persistence;

public sealed class OrderWorkflowRepository(
    ArchVizDbContext db, IUserDirectory users) : IOrderWorkflowRepository
{
    public async Task<DeliveryResult> RequestTransferAsync(int orderId, string managerId,
        string targetManagerId, string reason, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var order = await LockOrderAsync(orderId, ct);
        if (order?.ManagerId != managerId)
        {
            return new(DeliveryStatus.NotFound);
        }

        if (!IsOpen(order) || managerId == targetManagerId ||
            await HasPendingAsync(orderId, OrderActionKind.Transfer, ct) ||
            !await users.IsInRoleAsync(targetManagerId, "Manager"))
        {
            return new(DeliveryStatus.Conflict);
        }

        var request = new OrderActionRequest(orderId, OrderActionKind.Transfer,
            managerId, reason, targetManagerId);
        db.OrderActionRequests.Add(request);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return new(DeliveryStatus.Success, request.Id);
    }

    public async Task<DeliveryResult> RequestCancellationAsync(
        int orderId, string clientId, string reason, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var order = await LockOrderAsync(orderId, ct);
        if (order?.ClientId != clientId)
        {
            return new(DeliveryStatus.NotFound);
        }

        if (!IsOpen(order) || await HasPendingAsync(orderId, OrderActionKind.Cancellation, ct))
        {
            return new(DeliveryStatus.Conflict);
        }

        var request = new OrderActionRequest(
            orderId, OrderActionKind.Cancellation, clientId, reason);
        if (order.ManagerId is null)
        {
            order.Cancel();
            request.Decide(clientId, accept: true);
        }

        db.OrderActionRequests.Add(request);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return new(DeliveryStatus.Success, request.Id);
    }

    public async Task<IReadOnlyList<OrderActionResponse>> ListRequestsAsync(
        string userId, CancellationToken ct)
    {
        var rows = await db.OrderActionRequests.Where(request =>
                request.RequestedById == userId ||
                request.TargetManagerId == userId ||
                db.Orders.Any(order => order.Id == request.OrderId && order.ManagerId == userId))
            .OrderByDescending(request => request.Id)
            .ToListAsync(ct);

        return rows.Select(request => new OrderActionResponse(
            request.Id, request.OrderId, request.Kind.ToString(), request.Status.ToString(),
            request.RequestedById, request.TargetManagerId, request.Reason,
            request.RequestedAt, request.DecidedAt)).ToArray();
    }

    public async Task<DeliveryResult> DecideAsync(
        int requestId, string userId, bool accept, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var orderId = await db.OrderActionRequests.Where(item => item.Id == requestId)
            .Select(item => (int?)item.OrderId).SingleOrDefaultAsync(ct);
        if (orderId is null)
        {
            return new(DeliveryStatus.NotFound);
        }

        var order = await LockOrderAsync(orderId.Value, ct);
        var request = await db.OrderActionRequests.SingleOrDefaultAsync(item => item.Id == requestId, ct);
        if (order is null || request is null ||
            (request.Kind == OrderActionKind.Transfer
                ? request.TargetManagerId : order.ManagerId) != userId)
        {
            return new(DeliveryStatus.NotFound);
        }

        if (!IsOpen(order) || request.Status != OrderActionStatus.Pending ||
            (request.Kind == OrderActionKind.Transfer && order.ManagerId != request.RequestedById))
        {
            return new(DeliveryStatus.Conflict);
        }

        if (accept)
        {
            if (request.Kind == OrderActionKind.Transfer)
            {
                order.TransferTo(userId);
            }
            else
            {
                order.Cancel();
                var pendingTransfers = await db.OrderActionRequests.Where(item =>
                    item.OrderId == order.Id && item.Kind == OrderActionKind.Transfer &&
                    item.Status == OrderActionStatus.Pending).ToListAsync(ct);
                foreach (var transfer in pendingTransfers)
                {
                    transfer.Decide(userId, accept: false);
                }
            }
        }

        request.Decide(userId, accept);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return new(DeliveryStatus.Success);
    }

    public async Task<DeliveryResult> ReassignAsync(int orderId, int sceneId, string managerId,
        string visualizerId, string reason, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var order = await LockOrderAsync(orderId, ct);
        if (order?.ManagerId != managerId)
        {
            return new(DeliveryStatus.NotFound);
        }

        var scenes = await db.Scenes
            .FromSqlInterpolated($"""SELECT * FROM "Scenes" WHERE "Id" = {sceneId} AND "OrderId" = {orderId} FOR UPDATE""")
            .AsNoTracking().ToListAsync(ct);
        var scene = scenes.SingleOrDefault();
        if (scene is null)
        {
            return new(DeliveryStatus.NotFound);
        }

        if (!IsOpen(order) || scene.Status is SceneStatus.Closed or SceneStatus.ClientAccepted ||
            !await users.IsInRoleAsync(visualizerId, "Visualizer"))
        {
            return new(DeliveryStatus.Conflict);
        }

        var previous = await db.SceneAssignments.SingleOrDefaultAsync(
            assignment => assignment.SceneId == sceneId && assignment.EndedAt == null, ct);
        if (previous is null || previous.VisualizerId == visualizerId)
        {
            return new(DeliveryStatus.Conflict);
        }

        previous.End(managerId, reason);
        await db.SaveChangesAsync(ct);

        var assignment = new SceneAssignment(sceneId, visualizerId);
        db.SceneAssignments.Add(assignment);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return new(DeliveryStatus.Success, assignment.Id);
    }

    private Task<bool> HasPendingAsync(int orderId, OrderActionKind kind, CancellationToken ct) =>
        db.OrderActionRequests.AnyAsync(request => request.OrderId == orderId &&
            request.Kind == kind && request.Status == OrderActionStatus.Pending, ct);

    private static bool IsOpen(Order order) =>
        order.Status is not (OrderStatus.Completed or OrderStatus.Cancelled);

    private async Task<Order?> LockOrderAsync(int orderId, CancellationToken ct)
    {
        var rows = await db.Orders
            .FromSqlInterpolated($"""SELECT * FROM "Orders" WHERE "Id" = {orderId} FOR UPDATE""")
            .ToListAsync(ct);
        return rows.SingleOrDefault();
    }
}
