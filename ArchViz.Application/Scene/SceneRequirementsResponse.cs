namespace ArchViz.Application.Scenes;

public record SceneRequirementsResponse(
    int Id,
    int SceneId,
    int VersionNumber,
    string Requirements);