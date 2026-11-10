namespace ArchViz.Application.Scenes;

public enum CreateSceneStatus
{
    Success,
    OrderNotFound,
    OrderNotInPlanning
}

public record CreateSceneResult(
    CreateSceneStatus Status,
    int? SceneId = null);