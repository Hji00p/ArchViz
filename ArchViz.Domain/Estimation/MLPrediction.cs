namespace ArchViz.Domain.Estimation;

public sealed class MLPrediction
{
    public int Id { get; private set; }
    public int AssessmentId { get; private set; }
    public int ModelVersionId { get; private set; }
    public decimal PredictedHours { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private MLPrediction()
    {
    }

    public MLPrediction(
        int assessmentId,
        int modelVersionId,
        decimal predictedHours)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(assessmentId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(modelVersionId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(predictedHours);

        AssessmentId = assessmentId;
        ModelVersionId = modelVersionId;
        PredictedHours = predictedHours;
        CreatedAt = DateTimeOffset.UtcNow;
    }
}