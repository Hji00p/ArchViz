namespace ArchViz.Application.Estimation;

public enum ApproveScenePlanStatus
{
    Success,
    NotFound,
    InvalidState,
    InvalidHours,
    AdjustmentReasonRequired
}

public record ApproveScenePlanResult(
    ApproveScenePlanStatus Status,
    int? ApprovalId = null);