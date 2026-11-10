using ArchViz.Domain.Orders;

namespace ArchViz.Application.Orders;

public sealed class ClaimOrderHandler
{
    private readonly IOrderRepository _orders;

    public ClaimOrderHandler(IOrderRepository orders)
    {
        _orders = orders;
    }

    public async Task<ClaimOrderResult> HandleAsync(
        int orderId,
        string managerId,
        CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdAsync(
            orderId,
            cancellationToken);

        if (order is null)
        {
            return ClaimOrderResult.NotFound;
        }

        if (order.Status != OrderStatus.Submitted ||
            order.ManagerId is not null)
        {
            return ClaimOrderResult.Unavailable;
        }

        order.Claim(managerId);

        var saved = await _orders.TrySaveClaimAsync(
            order,
            cancellationToken);

        return saved
            ? ClaimOrderResult.Success
            : ClaimOrderResult.Unavailable;
    }
}