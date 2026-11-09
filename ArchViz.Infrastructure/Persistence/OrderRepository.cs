using ArchViz.Application.Orders;
using ArchViz.Domain.Orders;
using Microsoft.EntityFrameworkCore;

namespace ArchViz.Infrastructure.Persistence;

public sealed class OrderRepository : IOrderRepository
{
    private readonly ArchVizDbContext _db;

    public OrderRepository(ArchVizDbContext db)
    {
        _db = db;
    }

    public async Task<int> AddAsync(
        Order order,
        CancellationToken cancellationToken)
    {
        _db.Orders.Add(order);
        await _db.SaveChangesAsync(cancellationToken);

        return order.Id;
    }

    public Task<Order?> GetByIdAsync(
    int id,
    CancellationToken cancellationToken)
    {
        return _db.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(order => order.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Order>> GetAllAsync(
    CancellationToken cancellationToken)
    {
        return await _db.Orders
            .AsNoTracking()
            .OrderByDescending(order => order.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> TrySaveClaimAsync(
    Order order,
    CancellationToken cancellationToken)
    {
        var updatedRows = await _db.Orders
            .Where(savedOrder =>
                savedOrder.Id == order.Id &&
                savedOrder.Status == OrderStatus.Submitted &&
                savedOrder.ManagerId == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(savedOrder => savedOrder.ManagerId, order.ManagerId)
                    .SetProperty(savedOrder => savedOrder.Status, order.Status),
                cancellationToken);

        return updatedRows == 1;
    }

    public async Task<IReadOnlyList<Order>> GetByClientIdAsync(
    string clientId,
    CancellationToken cancellationToken)
    {
        return await _db.Orders
            .AsNoTracking()
            .Where(order => order.ClientId == clientId)
            .OrderByDescending(order => order.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Order>> GetUnassignedAsync(
    CancellationToken cancellationToken)
    {
        return await _db.Orders
            .AsNoTracking()
            .Where(order =>
                order.Status == OrderStatus.Submitted &&
                order.ManagerId == null)
            .OrderBy(order => order.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Order>> GetByManagerIdAsync(
    string managerId,
    CancellationToken cancellationToken)
    {
        return await _db.Orders
            .AsNoTracking()
            .Where(order => order.ManagerId == managerId)
            .OrderByDescending(order => order.Id)
            .ToListAsync(cancellationToken);
    }
}