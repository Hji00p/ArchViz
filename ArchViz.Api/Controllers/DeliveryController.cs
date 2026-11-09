using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using ArchViz.Application.Scenes;
using ArchViz.Domain.Scenes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArchViz.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/orders/{orderId:int}")]
public sealed class DeliveryController(IDeliveryRepository delivery) : ControllerBase
{
    private const int MaxFileBytes = 10 * 1024 * 1024;
    private const int MaxRequestBytes = MaxFileBytes + 64 * 1024;
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    [Authorize(Roles = "Client")]
    [HttpPost("attachments")]
    [RequestSizeLimit(MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxFileBytes)]
    public async Task<IActionResult> AddAttachment(int orderId, IFormFile file, CancellationToken ct)
    {
        if (!IsValidFile(file))
        {
            return BadRequest(new { message = "Provide a nonempty file of up to 10 MiB." });
        }

        var result = await delivery.AddAttachmentAsync(orderId, UserId, await ReadFileAsync(file, ct), ct);
        return ToResponse(result, create: true);
    }

    [HttpGet("attachments")]
    public async Task<IActionResult> ListAttachments(int orderId, CancellationToken ct)
    {
        var files = await delivery.ListAttachmentsAsync(orderId, UserId, ct);
        return files is null ? NotFound() : Ok(files);
    }

    [HttpGet("attachments/{attachmentId:int}/file")]
    public async Task<IActionResult> DownloadAttachment(int orderId, int attachmentId, CancellationToken ct)
    {
        var file = await delivery.ReadAttachmentAsync(orderId, attachmentId, UserId, ct);
        return file is null ? NotFound() : File(file.Content, "application/octet-stream", file.Name);
    }

    [Authorize(Roles = "Visualizer")]
    [HttpPost("scenes/{sceneId:int}/renders")]
    [RequestSizeLimit(MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxFileBytes)]
    public async Task<IActionResult> AddRender(int orderId, int sceneId, IFormFile file, CancellationToken ct)
    {
        if (!IsValidFile(file))
        {
            return BadRequest(new { message = "Provide a nonempty file of up to 10 MiB." });
        }

        var result = await delivery.AddRenderAsync(orderId, sceneId, UserId, await ReadFileAsync(file, ct), ct);
        return ToResponse(result, create: true);
    }

    [HttpGet("scenes/{sceneId:int}/renders")]
    public async Task<IActionResult> ListRenders(int orderId, int sceneId, CancellationToken ct)
    {
        var renders = await delivery.ListRendersAsync(orderId, sceneId, UserId, ct);
        return renders is null ? NotFound() : Ok(renders);
    }

    [HttpGet("scenes/{sceneId:int}/renders/{renderId:int}/file")]
    public async Task<IActionResult> DownloadRender(int orderId, int sceneId, int renderId, CancellationToken ct)
    {
        var file = await delivery.ReadRenderAsync(orderId, sceneId, renderId, UserId, ct);
        return file is null ? NotFound() : File(file.Content, "application/octet-stream", file.Name);
    }

    [Authorize(Roles = "Manager")]
    [HttpPost("scenes/{sceneId:int}/renders/{renderId:int}/publish")]
    public async Task<IActionResult> Publish(int orderId, int sceneId, int renderId, CancellationToken ct)
    {
        return ToResponse(await delivery.ReviewAsync(
            orderId, sceneId, renderId, UserId, RenderAction.Publish, null, ct));
    }

    [Authorize(Roles = "Manager")]
    [HttpPost("scenes/{sceneId:int}/renders/{renderId:int}/internal-revisions")]
    public async Task<IActionResult> RequestInternalChanges(
        int orderId, int sceneId, int renderId, RequestRenderChanges request, CancellationToken ct)
    {
        return ToResponse(await delivery.ReviewAsync(
            orderId, sceneId, renderId, UserId, RenderAction.RequestInternalChanges, request.Comment, ct),
            create: true);
    }

    [Authorize(Roles = "Client")]
    [HttpPost("scenes/{sceneId:int}/renders/{renderId:int}/accept")]
    public async Task<IActionResult> Accept(int orderId, int sceneId, int renderId, CancellationToken ct)
    {
        return ToResponse(await delivery.ReviewAsync(
            orderId, sceneId, renderId, UserId, RenderAction.Accept, null, ct));
    }

    [Authorize(Roles = "Client")]
    [HttpPost("scenes/{sceneId:int}/renders/{renderId:int}/revisions")]
    public async Task<IActionResult> RequestClientChanges(
        int orderId, int sceneId, int renderId, RequestRenderChanges request, CancellationToken ct)
    {
        return ToResponse(await delivery.ReviewAsync(
            orderId, sceneId, renderId, UserId, RenderAction.RequestClientChanges, request.Comment, ct),
            create: true);
    }

    [HttpGet("scenes/{sceneId:int}/revisions")]
    public async Task<IActionResult> ListRevisions(int orderId, int sceneId, CancellationToken ct)
    {
        var revisions = await delivery.ListRevisionsAsync(orderId, sceneId, UserId, ct);
        return revisions is null ? NotFound() : Ok(revisions);
    }

    [Authorize(Roles = "Manager")]
    [HttpPost("scenes/{sceneId:int}/revisions/{revisionId:int}/classify")]
    public async Task<IActionResult> Classify(int orderId, int sceneId, int revisionId,
        ClassifyRevision request, CancellationToken ct)
    {
        var classification = request.Classification?.ToLowerInvariant() switch
        {
            "correction" => RevisionClassification.Correction,
            "scopechange" => RevisionClassification.ScopeChange,
            _ => (RevisionClassification?)null
        };

        if (classification is null ||
            (classification == RevisionClassification.ScopeChange &&
             string.IsNullOrWhiteSpace(request.Requirements)))
        {
            return BadRequest(new { message = "Choose Correction or ScopeChange and provide updated requirements for ScopeChange." });
        }

        return ToResponse(await delivery.ClassifyAsync(orderId, sceneId, revisionId, UserId,
            classification.Value, request.Reason, request.Requirements, ct));
    }

    [Authorize(Roles = "Manager")]
    [HttpPost("scenes/{sceneId:int}/close")]
    public async Task<IActionResult> CloseScene(int orderId, int sceneId, CancellationToken ct)
    {
        return ToResponse(await delivery.CloseSceneAsync(orderId, sceneId, UserId, ct));
    }

    [Authorize(Roles = "Manager")]
    [HttpPost("complete")]
    public async Task<IActionResult> CompleteOrder(int orderId, CancellationToken ct)
    {
        return ToResponse(await delivery.CompleteOrderAsync(orderId, UserId, ct));
    }

    private IActionResult ToResponse(DeliveryResult result, bool create = false) =>
        result.Status switch
        {
            DeliveryStatus.Success when create => StatusCode(201, new { id = result.Id }),
            DeliveryStatus.Success => NoContent(),
            DeliveryStatus.NotFound => NotFound(),
            DeliveryStatus.Conflict => Conflict(new { message = "This action is unavailable in the current state." }),
            _ => throw new InvalidOperationException("Unexpected delivery result.")
        };

    private static bool IsValidFile(IFormFile file) =>
        file.Length is > 0 and <= MaxFileBytes &&
        FileName(file) is { Length: > 0 and <= 200 } name &&
        !name.Any(char.IsControl);

    private static string FileName(IFormFile file) =>
        Path.GetFileName(file.FileName.Replace('\\', '/')).Trim();

    private static async Task<DeliveryFile> ReadFileAsync(IFormFile file, CancellationToken ct)
    {
        using var buffer = new MemoryStream((int)file.Length);
        await file.CopyToAsync(buffer, ct);
        return new DeliveryFile(FileName(file), buffer.ToArray());
    }
}

public record RequestRenderChanges([Required, StringLength(2000)] string Comment);
public record ClassifyRevision(
    [Required] string Classification,
    [Required, StringLength(2000)] string Reason,
    [StringLength(10000)] string? Requirements);
