using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using ArchViz.Application.Communication;
using ArchViz.Application.Scenes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArchViz.Api.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public sealed class CommunicationController(ICommunicationRepository communication) : ControllerBase
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    [HttpGet("orders/{orderId:int}/messages")]
    public Task<IActionResult> GetOrderMessages(int orderId, int? beforeId, CancellationToken ct) =>
        List(orderId, null, beforeId, ct);

    [HttpPost("orders/{orderId:int}/messages")]
    public Task<IActionResult> SendOrderMessage(int orderId, SendChatMessage request, CancellationToken ct) =>
        Send(orderId, null, request, ct);

    [HttpGet("orders/{orderId:int}/scenes/{sceneId:int}/messages")]
    public Task<IActionResult> GetSceneMessages(int orderId, int sceneId, int? beforeId, CancellationToken ct) =>
        List(orderId, sceneId, beforeId, ct);

    [HttpPost("orders/{orderId:int}/scenes/{sceneId:int}/messages")]
    public Task<IActionResult> SendSceneMessage(
        int orderId, int sceneId, SendChatMessage request, CancellationToken ct) =>
        Send(orderId, sceneId, request, ct);

    [HttpGet("notifications")]
    public async Task<IActionResult> GetNotifications(int? beforeId, CancellationToken ct) =>
        Ok(await communication.ListNotificationsAsync(UserId, beforeId, ct));

    [HttpPut("notifications/{notificationId:int}/read")]
    public async Task<IActionResult> ReadNotification(int notificationId, CancellationToken ct) =>
        await communication.MarkReadAsync(notificationId, UserId, ct) ? NoContent() : NotFound();

    private async Task<IActionResult> List(
        int orderId, int? sceneId, int? beforeId, CancellationToken ct)
    {
        var messages = await communication.ListMessagesAsync(orderId, sceneId, UserId, beforeId, ct);
        return messages is null ? NotFound() : Ok(messages);
    }

    private async Task<IActionResult> Send(
        int orderId, int? sceneId, SendChatMessage request, CancellationToken ct)
    {
        var result = await communication.SendAsync(orderId, sceneId, UserId, request.Content, ct);
        return result.Status switch
        {
            DeliveryStatus.Success => StatusCode(201, new { id = result.Id }),
            DeliveryStatus.NotFound => NotFound(),
            DeliveryStatus.Conflict => Conflict(new { message = "This conversation is read-only." }),
            _ => throw new InvalidOperationException("Unexpected chat result.")
        };
    }
}

public record SendChatMessage([Required, StringLength(4000)] string Content);
