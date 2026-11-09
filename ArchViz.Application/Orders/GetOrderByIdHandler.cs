
using ArchViz.Domain.Orders;

namespace ArchViz.Application.Orders;

public sealed class GetOrderByIdHandler
{
    private readonly IOrderRepository _orders;

    public GetOrderByIdHandler(IOrderRepository orders)
    {
        _orders = orders;
    }

    public async Task<OrderResponse?> HandleAsync(
    int id,
    string clientId,
    CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdAsync(id, cancellationToken);

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