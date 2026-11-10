
namespace ArchViz.Application.Orders;

public sealed class GetManagedOrderByIdHandler
{
    private readonly IOrderRepository _orders;

    public GetManagedOrderByIdHandler(IOrderRepository orders)
    {
        _orders = orders;
    }

    public async Task<OrderResponse?> HandleAsync(
        int orderId,
        string managerId,
        CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdAsync(
            orderId,
            cancellationToken);

        if (order is null || order.ManagerId != managerId)
        {
            return null;
        }

        return new OrderResponse(
            order.Id,
            order.Title,
            order.Description,
            order.Status.ToString());
    }
}