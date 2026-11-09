namespace ArchViz.Domain.Scenes;

public sealed class SceneAssignment
{
    public int Id { get; private set; }
    public int SceneId { get; private set; }
    public string VisualizerId { get; private set; }
    public DateTimeOffset AssignedAt { get; private set; }
    public DateTimeOffset? EndedAt { get; private set; }

    private SceneAssignment()
    {
        VisualizerId = string.Empty;
    }

    public SceneAssignment(int sceneId, string visualizerId)
    {
        if (sceneId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sceneId),
                "Scene ID must be greater than zero.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(visualizerId);

        SceneId = sceneId;
        VisualizerId = visualizerId;
        AssignedAt = DateTimeOffset.UtcNow;
    }
}