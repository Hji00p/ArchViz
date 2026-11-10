using ArchViz.Api.Contracts.Orders;
using ArchViz.Application.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;


namespace ArchViz.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly SubmitOrderHandler _handler;
    private readonly GetOrderByIdHandler _getOrderById;
    private readonly GetOrdersHandler _getOrders;
    private readonly GetOrderQueueHandler _getOrderQueue;
    private readonly ClaimOrderHandler _claimOrder;
    private readonly GetManagedOrdersHandler _getManagedOrders;
    private readonly GetManagedOrderByIdHandler _getManagedOrderById;

    public OrdersController(
    SubmitOrderHandler handler,
    GetOrderByIdHandler getOrderById,
    GetOrdersHandler getOrders,
    GetOrderQueueHandler getOrderQueue,
    ClaimOrderHandler claimOrder,
    GetManagedOrdersHandler getManagedOrders,
    GetManagedOrderByIdHandler getManagedOrderById)
    {
        _handler = handler;
        _getOrderById = getOrderById;
        _getOrders = getOrders;
        _getOrderQueue = getOrderQueue;
        _claimOrder = claimOrder;
        _getManagedOrders = getManagedOrders;
        _getManagedOrderById = getManagedOrderById;
    }

    [Authorize(Roles = "Client")]
    [HttpPost]
    public async Task<IActionResult> Submit(
    SubmitOrderRequest request,
    CancellationToken cancellationToken)
    {
        var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(clientId))
        {
            return Unauthorized();
        }

        var orderId = await _handler.HandleAsync(
            request.Title,
            request.Description,
            clientId,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = orderId },
            new { id = orderId });
    }

    [Authorize(Roles = "Client")]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(
    int id,
    CancellationToken cancellationToken)
    {
        var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(clientId))
        {
            return Unauthorized();
        }

        var order = await _getOrderById.HandleAsync(
            id,
            clientId,
            cancellationToken);

        if (order is null)
        {
            return NotFound();
        }

        return Ok(order);
    }

    [Authorize(Roles = "Client")]
    [HttpGet]
    public async Task<IActionResult> GetAll(
    CancellationToken cancellationToken)
    {
        var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(clientId))
        {
            return Unauthorized();
        }

        var orders = await _getOrders.HandleAsync(
            clientId,
            cancellationToken);

        return Ok(orders);
    }

    [Authorize(Roles = "Manager")]
    [HttpGet("queue")]
    public async Task<IActionResult> GetQueue(
    CancellationToken cancellationToken)
    {
        var orders = await _getOrderQueue.HandleAsync(cancellationToken);

        return Ok(orders);
    }

    [Authorize(Roles = "Manager")]
    [HttpPost("{id:int}/claim")]
    public async Task<IActionResult> Claim(
    int id,
    CancellationToken cancellationToken)
    {
        var managerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(managerId))
        {
            return Unauthorized();
        }

        var result = await _claimOrder.HandleAsync(
            id,
            managerId,
            cancellationToken);

        return result switch
        {
            ClaimOrderResult.Success => NoContent(),
            ClaimOrderResult.NotFound => NotFound(),
            ClaimOrderResult.Unavailable => Conflict(new
            {
                message = "Order is unavailable for claiming."
            }),
            _ => throw new InvalidOperationException(
                "Unexpected claim result.")
        };
    }

    [Authorize(Roles = "Manager")]
    [HttpGet("managed")]
    public async Task<IActionResult> GetManaged(
    CancellationToken cancellationToken)
    {
        var managerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(managerId))
        {
            return Unauthorized();
        }

        var orders = await _getManagedOrders.HandleAsync(
            managerId,
            cancellationToken);

        return Ok(orders);
    }

    [Authorize(Roles = "Manager")]
    [HttpGet("managed/{id:int}")]
    public async Task<IActionResult> GetManagedById(
    int id,
    CancellationToken cancellationToken)
    {
        var managerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(managerId))
        {
            return Unauthorized();
        }

        var order = await _getManagedOrderById.HandleAsync(
            id,
            managerId,
            cancellationToken);

        if (order is null)
        {
            return NotFound();
        }

        return Ok(order);
    }
}