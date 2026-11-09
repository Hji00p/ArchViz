namespace ArchViz.Domain.Orders;

public enum OrderActionKind { Transfer, Cancellation }
public enum OrderActionStatus { Pending, Accepted, Rejected }

public sealed class OrderActionRequest
{
    public int Id { get; private set; }
    public int OrderId { get; private set; }
    public OrderActionKind Kind { get; private set; }
    public OrderActionStatus Status { get; private set; }
    public string RequestedById { get; private set; } = string.Empty;
    public string? TargetManagerId { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public DateTimeOffset RequestedAt { get; private set; }
    public string? DecidedById { get; private set; }
    public DateTimeOffset? DecidedAt { get; private set; }

    private OrderActionRequest() { }

    public OrderActionRequest(int orderId, OrderActionKind kind, string requesterId,
        string reason, string? targetManagerId = null)
    {
        OrderId = orderId;
        Kind = kind;
        RequestedById = requesterId;
        Reason = reason.Trim();
        TargetManagerId = targetManagerId;
        RequestedAt = DateTimeOffset.UtcNow;
    }

    public void Decide(string userId, bool accept)
    {
        if (Status != OrderActionStatus.Pending)
        {
            throw new InvalidOperationException("This request has already been decided.");
        }

        Status = accept ? OrderActionStatus.Accepted : OrderActionStatus.Rejected;
        DecidedById = userId;
        DecidedAt = DateTimeOffset.UtcNow;
    }
}
