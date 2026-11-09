using System.Security.Claims;
using ArchViz.Application.Scenes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArchViz.Api.Controllers;

[ApiController]
[Authorize]
public sealed class SceneTimeController(ISceneTimeRepository repository) : ControllerBase
{
    [Authorize(Roles = "Visualizer")]
    [HttpGet("api/scenes/mine/{sceneId:int}/time-entries")]
    public async Task<IActionResult> GetOwn(int sceneId, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var entries = await repository.GetOwnAsync(sceneId, userId, cancellationToken);
        return entries is null ? NotFound() : Ok(entries);
    }

    [Authorize(Roles = "Visualizer")]
    [HttpPost("api/scenes/mine/{sceneId:int}/time-entries")]
    public Task<IActionResult> Add(
        int sceneId, TimeEntryInput input, CancellationToken cancellationToken)
    {
        return Save(sceneId, null, input, cancellationToken);
    }

    [Authorize(Roles = "Visualizer")]
    [HttpPut("api/scenes/mine/{sceneId:int}/time-entries/{entryId:int}")]
    public Task<IActionResult> Edit(
        int sceneId, int entryId, TimeEntryInput input, CancellationToken cancellationToken)
    {
        return Save(sceneId, entryId, input, cancellationToken);
    }

    [Authorize(Roles = "Manager")]
    [HttpGet("api/orders/{orderId:int}/scenes/{sceneId:int}/time-summary")]
    public async Task<IActionResult> GetSummary(
        int orderId, int sceneId, CancellationToken cancellationToken)
    {
        var managerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(managerId))
        {
            return Unauthorized();
        }

        var summary = await repository.GetSummaryAsync(
            orderId, sceneId, managerId, cancellationToken);
        return summary is null ? NotFound() : Ok(summary);
    }

    private async Task<IActionResult> Save(
        int sceneId, int? entryId, TimeEntryInput input, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        if (input.WorkDate == default || input.Hours <= 0 ||
            string.IsNullOrWhiteSpace(input.Description))
        {
            return BadRequest(new { message = "Provide a work date, positive hours, and a description." });
        }

        var result = await repository.SaveAsync(sceneId, entryId, userId, input, cancellationToken);

        return result.Status switch
        {
            TimeEntryStatus.Success when entryId is null =>
                StatusCode(StatusCodes.Status201Created, result.Entry),
            TimeEntryStatus.Success => Ok(result.Entry),
            TimeEntryStatus.NotFound => NotFound(),
            TimeEntryStatus.InvalidState =>
                Conflict(new { message = "Time cannot be recorded or edited in the current state." }),
            _ => throw new InvalidOperationException("Unexpected time entry result.")
        };
    }
}
