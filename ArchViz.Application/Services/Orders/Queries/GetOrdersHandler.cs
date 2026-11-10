
namespace ArchViz.Application.Orders;

public sealed class GetOrdersHandler
{
    private readonly IOrderRepository _orders;

    public GetOrdersHandler(IOrderRepository orders)
    {
        _orders = orders;
    }

    public async Task<IReadOnlyList<OrderResponse>> HandleAsync(
        string clientId,
        CancellationToken cancellationToken)
    {
        var orders = await _orders.GetByClientIdAsync(
            clientId,
            cancellationToken);

        return orders
            .Select(order => new OrderResponse(
                order.Id,
                order.Title,
                order.Description,
                order.Status.ToString()))
            .ToList();
    }
}