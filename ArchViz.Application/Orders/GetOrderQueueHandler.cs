namespace ArchViz.Application.Orders;

public sealed class GetOrderQueueHandler
{
    private readonly IOrderRepository _orders;

    public GetOrderQueueHandler(IOrderRepository orders)
    {
        _orders = orders;
    }

    public async Task<IReadOnlyList<OrderQueueItemResponse>> HandleAsync(
        CancellationToken cancellationToken)
    {
        var orders = await _orders.GetUnassignedAsync(cancellationToken);

        return orders
            .Select(order => new OrderQueueItemResponse(
                order.Id,
                order.Title,
                order.Description.Length > 300
                    ? order.Description[..300] + "…"
                    : order.Description))
            .ToList();
    }
}