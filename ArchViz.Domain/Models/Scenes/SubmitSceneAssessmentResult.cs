namespace ArchViz.Application.Scenes;

public enum SubmitSceneAssessmentStatus
{
    Success,
    NotFound,
    Conflict
}

public record SubmitSceneAssessmentResult(
    SubmitSceneAssessmentStatus Status,
    int? AssessmentId = null);