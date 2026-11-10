namespace ArchViz.Application.Scenes;

public record SceneAssessmentResponse(
    int Id,
    int SceneId,
    int RequirementsRevisionId,
    int VersionNumber,
    int ViewsCount,
    int ModelingComplexity,
    decimal CustomModelingRatio,
    int MaterialsComplexity,
    int LightingComplexity,
    int PostProcessingComplexity,
    decimal PredictedHours);