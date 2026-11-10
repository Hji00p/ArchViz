namespace ArchViz.Domain.Orders;

public sealed class Order
{
    public int Id { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Submitted;
    public string? ManagerId { get; private set; }
    public string? ClientId { get; private set; }



    public Order(
    string title,
    string description,
    string clientId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);

        var trimmedTitle = title.Trim();

        if (trimmedTitle.Length > 200)
        {
            throw new ArgumentException(
                "Order title must not exceed 200 characters.",
                nameof(title));
        }

        Title = trimmedTitle;
        Description = description.Trim();
        ClientId = clientId;
    }



    public void Claim(string managerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(managerId);

        if (Status != OrderStatus.Submitted || ManagerId is not null)
        {
            throw new InvalidOperationException(
                "Замовлення недоступне для взяття.");
        }

        ManagerId = managerId;
        Status = OrderStatus.InPlanning;
    }

    public DateTimeOffset? CompletedAt { get; private set; }

    public void Complete()
    {
        if (Status is not (OrderStatus.InPlanning or OrderStatus.InProgress))
        {
            throw new InvalidOperationException("Only an active order can be completed.");
        }

        Status = OrderStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void TransferTo(string managerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(managerId);
        if (Status is not (OrderStatus.InPlanning or OrderStatus.InProgress))
        {
            throw new InvalidOperationException("Only an active order can be transferred.");
        }

        ManagerId = managerId;
    }

    public void Cancel()
    {
        if (Status is OrderStatus.Cancelled or OrderStatus.Completed)
        {
            throw new InvalidOperationException("The order cannot be cancelled.");
        }

        Status = OrderStatus.Cancelled;
    }

    private Order()
    {
        Title = string.Empty;
        Description = string.Empty;
    }
}