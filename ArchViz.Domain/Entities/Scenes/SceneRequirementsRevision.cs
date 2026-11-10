namespace ArchViz.Domain.Scenes;

public sealed class SceneRequirementsRevision
{
    public int Id { get; private set; }
    public int SceneId { get; private set; }
    public int VersionNumber { get; private set; }
    public string RequirementsSnapshot { get; private set; }
    public string CreatedById { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private SceneRequirementsRevision()
    {
        RequirementsSnapshot = string.Empty;
        CreatedById = string.Empty;
    }

    public SceneRequirementsRevision(
        int sceneId,
        int versionNumber,
        string requirements,
        string createdById)
    {
        if (sceneId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sceneId));
        }

        if (versionNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(versionNumber));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(requirements);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdById);

        var trimmedRequirements = requirements.Trim();

        if (trimmedRequirements.Length > 10000)
        {
            throw new ArgumentException(
                "Scene requirements must not exceed 10000 characters.",
                nameof(requirements));
        }

        SceneId = sceneId;
        VersionNumber = versionNumber;
        RequirementsSnapshot = trimmedRequirements;
        CreatedById = createdById;
        CreatedAt = DateTimeOffset.UtcNow;
    }
}