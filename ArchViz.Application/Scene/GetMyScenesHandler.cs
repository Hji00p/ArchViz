namespace ArchViz.Application.Scenes;

public sealed class GetMyScenesHandler
{
    private readonly ISceneRepository _scenes;

    public GetMyScenesHandler(ISceneRepository scenes)
    {
        _scenes = scenes;
    }

    public async Task<IReadOnlyList<SceneResponse>> HandleAsync(
        string visualizerId,
        CancellationToken cancellationToken)
    {
        var scenes = await _scenes.GetAssignedToAsync(
            visualizerId,
            cancellationToken);

        return scenes
            .Select(scene => new SceneResponse(
                scene.Id,
                scene.OrderId,
                scene.Name,
                scene.Status.ToString()))
            .ToList();
    }
}