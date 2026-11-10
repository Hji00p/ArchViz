namespace ArchViz.Application.Estimation;

public enum SaveScenePredictionStatus
{
    Success,
    AssessmentNotFound,
    ModelNotRegistered
}

public record SaveScenePredictionResult(
    SaveScenePredictionStatus Status,
    int? PredictionId = null,
    decimal? PredictedHours = null);