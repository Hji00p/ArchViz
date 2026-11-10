namespace ArchViz.Application.Scenes;

public enum AddSceneRequirementsStatus
{
    Success,
    NotFound,
    InvalidState,
    Conflict
}

public record AddSceneRequirementsResult(
    AddSceneRequirementsStatus Status,
    int? RevisionId = null,
    int? VersionNumber = null);