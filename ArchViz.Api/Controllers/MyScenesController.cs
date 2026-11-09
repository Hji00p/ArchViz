using System.Security.Claims;
using ArchViz.Application.Scenes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ArchViz.Api.Contracts.Scenes;

namespace ArchViz.Api.Controllers;

[ApiController]
[Authorize(Roles = "Visualizer")]
[Route("api/scenes/mine")]
public class MyScenesController : ControllerBase
{
    private readonly GetMyScenesHandler _getMyScenes;
    private readonly GetMySceneRequirementsHandler _getRequirements;
    private readonly SubmitSceneAssessmentHandler _submitAssessment;


    public MyScenesController(
        GetMyScenesHandler getMyScenes,
        GetMySceneRequirementsHandler getRequirements,
        SubmitSceneAssessmentHandler submitAssessment)
    {
        _getMyScenes = getMyScenes;
        _getRequirements = getRequirements;
        _submitAssessment = submitAssessment;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken)
    {
        var visualizerId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(visualizerId))
        {
            return Unauthorized();
        }

        var scenes = await _getMyScenes.HandleAsync(
            visualizerId,
            cancellationToken);

        return Ok(scenes);
    }

    [HttpGet("{sceneId:int}/requirements")]
    public async Task<IActionResult> GetRequirements(
    int sceneId,
    CancellationToken cancellationToken)
    {
        var visualizerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(visualizerId))
        {
            return Unauthorized();
        }

        var requirements = await _getRequirements.HandleAsync(
            sceneId,
            visualizerId,
            cancellationToken);

        if (requirements is null)
        {
            return NotFound();
        }

        return Ok(requirements);
    }

    [HttpPost("{sceneId:int}/assessments")]
    public async Task<IActionResult> SubmitAssessment(
    int sceneId,
    SubmitSceneAssessmentRequest request,
    CancellationToken cancellationToken)
    {
        var visualizerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(visualizerId))
        {
            return Unauthorized();
        }

        var input = new SubmitSceneAssessmentInput(
            request.RequirementsRevisionId,
            request.ViewsCount,
            request.ModelingComplexity,
            request.CustomModelingRatio!.Value,
            request.MaterialsComplexity,
            request.LightingComplexity,
            request.PostProcessingComplexity);

        var result = await _submitAssessment.HandleAsync(
            sceneId, visualizerId, input, cancellationToken);

        return result.Status switch
        {
            SubmitSceneAssessmentStatus.Success => StatusCode(
                StatusCodes.Status201Created,
                new { id = result.AssessmentId }),

            SubmitSceneAssessmentStatus.NotFound => NotFound(),

            SubmitSceneAssessmentStatus.Conflict => Conflict(new
            {
                message = "The assessment cannot be submitted. Reload the scene and requirements."
            }),

            _ => throw new InvalidOperationException(
                "Unexpected assessment submission result.")
        };
    }

    [HttpPost("{sceneId:int}/start")]
    public async Task<IActionResult> StartWork(
    int sceneId,
    [FromServices] StartSceneWorkHandler handler,
    CancellationToken cancellationToken)
    {
        var visualizerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(visualizerId))
        {
            return Unauthorized();
        }

        var result = await handler.HandleAsync(
            sceneId, visualizerId, cancellationToken);

        return result switch
        {
            StartSceneWorkResult.Success => NoContent(),
            StartSceneWorkResult.NotFound => NotFound(),
            StartSceneWorkResult.Conflict => Conflict(new
            {
                message = "The scene could not be started. Reload it and try again."
            }),
            _ => throw new InvalidOperationException("Unexpected start result.")
        };
    }
}