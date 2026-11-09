using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using ArchViz.Application.Orders;
using ArchViz.Application.Scenes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArchViz.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/orders")]
public sealed class OrderWorkflowController(IOrderWorkflowRepository workflow) : ControllerBase
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    [Authorize(Roles = "Manager")]
    [HttpPost("{orderId:int}/transfer-requests")]
    public async Task<IActionResult> Transfer(
        int orderId, RequestOrderTransfer request, CancellationToken ct) =>
        ToResponse(await workflow.RequestTransferAsync(
            orderId, UserId, request.TargetManagerId, request.Reason, ct), create: true);

    [Authorize(Roles = "Client")]
    [HttpPost("{orderId:int}/cancellation-requests")]
    public async Task<IActionResult> Cancel(
        int orderId, RequestOrderCancellation request, CancellationToken ct) =>
        ToResponse(await workflow.RequestCancellationAsync(orderId, UserId, request.Reason, ct), create: true);

    [HttpGet("workflow-requests")]
    public async Task<IActionResult> ListRequests(CancellationToken ct) =>
        Ok(await workflow.ListRequestsAsync(UserId, ct));

    [Authorize(Roles = "Manager")]
    [HttpPost("workflow-requests/{requestId:int}/decision")]
    public async Task<IActionResult> Decide(
        int requestId, DecideOrderRequest request, CancellationToken ct) =>
        ToResponse(await workflow.DecideAsync(requestId, UserId, request.Accept!.Value, ct));

    [Authorize(Roles = "Manager")]
    [HttpPost("{orderId:int}/scenes/{sceneId:int}/reassign")]
    public async Task<IActionResult> Reassign(
        int orderId, int sceneId, ReassignScene request, CancellationToken ct) =>
        ToResponse(await workflow.ReassignAsync(
            orderId, sceneId, UserId, request.VisualizerId, request.Reason, ct), create: true);

    private IActionResult ToResponse(DeliveryResult result, bool create = false) =>
        result.Status switch
        {
            DeliveryStatus.Success when create => StatusCode(201, new { id = result.Id }),
            DeliveryStatus.Success => NoContent(),
            DeliveryStatus.NotFound => NotFound(),
            DeliveryStatus.Conflict => Conflict(new { message = "The requested change is unavailable." }),
            _ => throw new InvalidOperationException("Unexpected workflow result.")
        };
}

public record RequestOrderTransfer(
    [Required] string TargetManagerId, [Required, StringLength(2000)] string Reason);
public record RequestOrderCancellation([Required, StringLength(2000)] string Reason);
public record DecideOrderRequest([Required] bool? Accept);
public record ReassignScene(
    [Required] string VisualizerId, [Required, StringLength(2000)] string Reason);
