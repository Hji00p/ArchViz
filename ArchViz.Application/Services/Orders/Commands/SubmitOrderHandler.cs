using ArchViz.Domain.Orders;

namespace ArchViz.Application.Orders;

public sealed class SubmitOrderHandler
{
    private readonly IOrderRepository _orders;

    public SubmitOrderHandler(IOrderRepository orders)
    {
        _orders = orders;
    }

    public async Task<int> HandleAsync(
        string title,
        string description,
        string clientId,
        CancellationToken cancellationToken)
    {
        var order = new Order(title, description, clientId);

        return await _orders.AddAsync(order, cancellationToken);
    }
}