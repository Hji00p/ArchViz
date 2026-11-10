using System.ComponentModel.DataAnnotations;

namespace ArchViz.Api.Contracts.Scenes;

public record ApproveScenePlanRequest(
    [Range(1, int.MaxValue)] int PredictionId,
    decimal ApprovedHours,
    string? AdjustmentReason = null);