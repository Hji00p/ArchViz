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

    private Order()
    {
        Title = string.Empty;
        Description = string.Empty;
    }
}