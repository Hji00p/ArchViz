namespace ArchViz.Application.Scenes;

public record SubmitSceneAssessmentInput(
    int RequirementsRevisionId,
    int ViewsCount,
    int ModelingComplexity,
    decimal CustomModelingRatio,
    int MaterialsComplexity,
    int LightingComplexity,
    int PostProcessingComplexity);