using ArchViz.Domain.Orders;

namespace ArchViz.Application.Orders;

public interface IOrderRepository
{
    Task<IReadOnlyList<Order>> GetByManagerIdAsync(
    string managerId,
    CancellationToken cancellationToken);
    Task<int> AddAsync(
        Order order,
        CancellationToken cancellationToken);

    Task<Order?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Order>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<bool> TrySaveClaimAsync(
    Order order,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Order>> GetByClientIdAsync(
    string clientId,
    CancellationToken cancellationToken);

    Task<IReadOnlyList<Order>> GetUnassignedAsync(
    CancellationToken cancellationToken);
}