namespace ArchViz.Application.Orders;

public sealed class GetManagedOrdersHandler
{
    private readonly IOrderRepository _orders;

    public GetManagedOrdersHandler(IOrderRepository orders)
    {
        _orders = orders;
    }

    public async Task<IReadOnlyList<OrderResponse>> HandleAsync(
        string managerId,
        CancellationToken cancellationToken)
    {
        var orders = await _orders.GetByManagerIdAsync(
            managerId,
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