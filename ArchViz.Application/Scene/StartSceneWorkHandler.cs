using ArchViz.Domain.Scenes;

namespace ArchViz.Application.Scenes;

public enum StartSceneWorkResult
{
    Success,
    NotFound,
    Conflict
}

public sealed class StartSceneWorkHandler
{
    private readonly ISceneRepository _scenes;

    public StartSceneWorkHandler(ISceneRepository scenes)
    {
        _scenes = scenes;
    }

    public async Task<StartSceneWorkResult> HandleAsync(
        int sceneId,
        string visualizerId,
        CancellationToken cancellationToken)
    {
        var scene = await _scenes.GetAssignedSceneAsync(
            sceneId, visualizerId, cancellationToken);

        if (scene is null)
        {
            return StartSceneWorkResult.NotFound;
        }

        if (scene.Status != SceneStatus.Ready)
        {
            return StartSceneWorkResult.Conflict;
        }

        scene.StartWork();

        var saved = await _scenes.TryStartWorkAsync(
            scene, visualizerId, cancellationToken);

        return saved
            ? StartSceneWorkResult.Success
            : StartSceneWorkResult.Conflict;
    }
}