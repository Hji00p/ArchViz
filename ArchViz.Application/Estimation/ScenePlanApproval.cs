namespace ArchViz.Domain.Estimation;

public sealed class ScenePlanApproval
{
    public int Id { get; private set; }
    public int PredictionId { get; private set; }
    public string ApprovedById { get; private set; }
    public decimal ApprovedHours { get; private set; }
    public string? AdjustmentReason { get; private set; }
    public DateTimeOffset ApprovedAt { get; private set; }

    private ScenePlanApproval()
    {
        ApprovedById = string.Empty;
    }

    public ScenePlanApproval(
        int predictionId,
        string approvedById,
        decimal approvedHours,
        string? adjustmentReason)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(predictionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(approvedById);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(approvedHours);

        PredictionId = predictionId;
        ApprovedById = approvedById;
        ApprovedHours = approvedHours;
        AdjustmentReason = string.IsNullOrWhiteSpace(adjustmentReason)
            ? null
            : adjustmentReason.Trim();
        ApprovedAt = DateTimeOffset.UtcNow;
    }
}