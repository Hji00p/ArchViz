namespace ArchViz.Application.Scenes;

public sealed class GetMySceneRequirementsHandler
{
    private readonly ISceneRepository _scenes;

    public GetMySceneRequirementsHandler(ISceneRepository scenes)
    {
        _scenes = scenes;
    }

    public async Task<SceneRequirementsResponse?> HandleAsync(
        int sceneId,
        string visualizerId,
        CancellationToken cancellationToken)
    {
        var revision = await _scenes.GetLatestAssignedRequirementsAsync(
            sceneId,
            visualizerId,
            cancellationToken);

        if (revision is null)
        {
            return null;
        }

        return new SceneRequirementsResponse(
            revision.Id,
            revision.SceneId,
            revision.VersionNumber,
            revision.RequirementsSnapshot);
    }
}