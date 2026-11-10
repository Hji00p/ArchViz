namespace ArchViz.Domain.Scenes;

public sealed class SceneAssessment
{
    public int Id { get; private set; }
    public int RequirementsRevisionId { get; private set; }
    public string AssessedById { get; private set; }
    public int ViewsCount { get; private set; }
    public int ModelingComplexity { get; private set; }
    public decimal CustomModelingRatio { get; private set; }
    public int MaterialsComplexity { get; private set; }
    public int LightingComplexity { get; private set; }
    public int PostProcessingComplexity { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private SceneAssessment()
    {
        AssessedById = string.Empty;
    }

    public SceneAssessment(
        int requirementsRevisionId,
        string assessedById,
        int viewsCount,
        int modelingComplexity,
        decimal customModelingRatio,
        int materialsComplexity,
        int lightingComplexity,
        int postProcessingComplexity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
            requirementsRevisionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(assessedById);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(viewsCount);

        ValidateComplexity(modelingComplexity, nameof(modelingComplexity));
        ValidateComplexity(materialsComplexity, nameof(materialsComplexity));
        ValidateComplexity(lightingComplexity, nameof(lightingComplexity));
        ValidateComplexity(
            postProcessingComplexity,
            nameof(postProcessingComplexity));

        if (customModelingRatio is < 0m or > 1m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(customModelingRatio),
                "Custom modeling ratio must be between 0 and 1.");
        }

        RequirementsRevisionId = requirementsRevisionId;
        AssessedById = assessedById;
        ViewsCount = viewsCount;
        ModelingComplexity = modelingComplexity;
        CustomModelingRatio = customModelingRatio;
        MaterialsComplexity = materialsComplexity;
        LightingComplexity = lightingComplexity;
        PostProcessingComplexity = postProcessingComplexity;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    private static void ValidateComplexity(int value, string parameterName)
    {
        if (value is < 1 or > 5)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Complexity must be between 1 and 5.");
        }
    }
}