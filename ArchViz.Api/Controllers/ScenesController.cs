using System.Security.Claims;
using ArchViz.Api.Contracts.Scenes;
using ArchViz.Application.Estimation;
using ArchViz.Application.Scenes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace ArchViz.Api.Controllers;

[ApiController]
[Authorize(Roles = "Manager")]
[Route("api/orders/{orderId:int}/scenes")]
public class ScenesController : ControllerBase
{
    private readonly CreateSceneHandler _createScene;
    private readonly GetOrderScenesHandler _getOrderScenes;
    private readonly AssignVisualizerHandler _assignVisualizer;
    private readonly AddSceneRequirementsHandler _addRequirements;
    private readonly GetSceneAssessmentHandler _getSceneAssessment;

    public ScenesController(
    CreateSceneHandler createScene,
    GetOrderScenesHandler getOrderScenes,
    AssignVisualizerHandler assignVisualizer,
    AddSceneRequirementsHandler addRequirements,
    GetSceneAssessmentHandler getSceneAssessment)
    {
        _createScene = createScene;
        _getOrderScenes = getOrderScenes;
        _assignVisualizer = assignVisualizer;
        _addRequirements = addRequirements;
        _getSceneAssessment = getSceneAssessment;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        int orderId,
        CreateSceneRequest request,
        CancellationToken cancellationToken)
    {
        var managerId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(managerId))
        {
            return Unauthorized();
        }

        var result = await _createScene.HandleAsync(
            orderId,
            request.Name,
            managerId,
            cancellationToken);

        return result.Status switch
        {
            CreateSceneStatus.Success => StatusCode(
                StatusCodes.Status201Created,
                new { id = result.SceneId }),

            CreateSceneStatus.OrderNotFound => NotFound(),

            CreateSceneStatus.OrderNotInPlanning => Conflict(new
            {
                message = "Scenes can only be created for orders in planning."
            }),

            _ => throw new InvalidOperationException(
                "Unexpected scene creation result.")
        };
    }

    [HttpGet]
    public async Task<IActionResult> GetByOrderId(
    int orderId,
    CancellationToken cancellationToken)
    {
        var managerId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(managerId))
        {
            return Unauthorized();
        }

        var scenes = await _getOrderScenes.HandleAsync(
            orderId,
            managerId,
            cancellationToken);

        if (scenes is null)
        {
            return NotFound();
        }

        return Ok(scenes);
    }

    [HttpPost("{sceneId:int}/assignments")]
    public async Task<IActionResult> AssignVisualizer(
    int orderId,
    int sceneId,
    AssignVisualizerRequest request,
    CancellationToken cancellationToken)
    {
        var managerId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(managerId))
        {
            return Unauthorized();
        }

        var result = await _assignVisualizer.HandleAsync(
            orderId,
            sceneId,
            request.VisualizerId,
            managerId,
            cancellationToken);

        return result.Status switch
        {
            AssignVisualizerStatus.Success => StatusCode(
                StatusCodes.Status201Created,
                new { id = result.AssignmentId }),

            AssignVisualizerStatus.NotFound => NotFound(),

            AssignVisualizerStatus.InvalidState => Conflict(new
            {
                message = "The order or scene is not ready for assignment."
            }),

            AssignVisualizerStatus.InvalidVisualizer => BadRequest(new
            {
                message = "The user must have the Visualizer role."
            }),

            AssignVisualizerStatus.AlreadyAssigned => Conflict(new
            {
                message = "The scene already has an active assignment."
            }),

            _ => throw new InvalidOperationException(
                "Unexpected assignment result.")
        };
    }

    [HttpPost("{sceneId:int}/requirements")]
    public async Task<IActionResult> AddRequirements(
    int orderId,
    int sceneId,
    AddSceneRequirementsRequest request,
    CancellationToken cancellationToken)
    {
        var managerId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(managerId))
        {
            return Unauthorized();
        }

        var result = await _addRequirements.HandleAsync(
            orderId,
            sceneId,
            request.Requirements,
            managerId,
            cancellationToken);

        return result.Status switch
        {
            AddSceneRequirementsStatus.Success => StatusCode(
                StatusCodes.Status201Created,
                new
                {
                    id = result.RevisionId,
                    versionNumber = result.VersionNumber
                }),

            AddSceneRequirementsStatus.NotFound => NotFound(),

            AddSceneRequirementsStatus.InvalidState => Conflict(new
            {
                message = "Requirements cannot be changed in the current state."
            }),

            AddSceneRequirementsStatus.Conflict => Conflict(new
            {
                message = "Requirements were updated concurrently. Reload and retry."
            }),

            _ => throw new InvalidOperationException(
                "Unexpected requirements result.")
        };
    }

    [HttpGet("{sceneId:int}/assessment")]
    public async Task<IActionResult> GetAssessment(
    int orderId,
    int sceneId,
    CancellationToken cancellationToken)
    {
        var managerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(managerId))
        {
            return Unauthorized();
        }

        var assessment = await _getSceneAssessment.HandleAsync(
            orderId, sceneId, managerId, cancellationToken);

        if (assessment is null)
        {
            return NotFound();
        }

        return Ok(assessment);
    }

    [HttpPost("{sceneId:int}/predictions")]
    public async Task<IActionResult> SavePrediction(
    int orderId,
    int sceneId,
    [FromServices] SaveScenePredictionHandler handler,
    CancellationToken cancellationToken)
    {
        var managerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(managerId))
        {
            return Unauthorized();
        }

        var result = await handler.HandleAsync(
            orderId, sceneId, managerId, cancellationToken);

        return result.Status switch
        {
            SaveScenePredictionStatus.Success => Ok(new
            {
                id = result.PredictionId,
                predictedHours = result.PredictedHours
            }),

            SaveScenePredictionStatus.AssessmentNotFound => NotFound(),

            SaveScenePredictionStatus.ModelNotRegistered => StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new { message = "The prediction model is not registered." }),

            _ => throw new InvalidOperationException(
                "Unexpected prediction result.")
        };
    }

    [HttpPost("{sceneId:int}/plan-approvals")]
    public async Task<IActionResult> ApprovePlan(
    int orderId,
    int sceneId,
    ApproveScenePlanRequest request,
    [FromServices] ApproveScenePlanHandler handler,
    CancellationToken cancellationToken)
    {
        var managerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(managerId))
        {
            return Unauthorized();
        }

        var result = await handler.HandleAsync(
            orderId,
            sceneId,
            managerId,
            request.PredictionId,
            request.ApprovedHours,
            request.AdjustmentReason,
            cancellationToken);

        return result.Status switch
        {
            ApproveScenePlanStatus.Success => StatusCode(
                StatusCodes.Status201Created,
                new { id = result.ApprovalId }),

            ApproveScenePlanStatus.NotFound => NotFound(),

            ApproveScenePlanStatus.InvalidState => Conflict(new
            {
                message = "The scene is not awaiting plan approval."
            }),

            ApproveScenePlanStatus.InvalidHours => BadRequest(new
            {
                message = "Approved hours must be greater than zero."
            }),

            ApproveScenePlanStatus.AdjustmentReasonRequired => BadRequest(new
            {
                message = "An adjustment reason is required when approved hours differ from the prediction."
            }),

            _ => throw new InvalidOperationException(
                "Unexpected plan approval result.")
        };
    }
}