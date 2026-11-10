
namespace ArchViz.Application.Orders;

public sealed class GetOrderByIdHandler
{
    private readonly IOrderRepository _orders;

    public GetOrderByIdHandler(IOrderRepository orders)
    {
        _orders = orders;
    }

    public async Task<OrderResponse?> HandleAsync(
        int orderId,
        string clientId,
        CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdAsync(orderId, cancellationToken);

        if (order is null || order.ClientId != clientId)
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