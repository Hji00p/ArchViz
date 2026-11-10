using System.ComponentModel.DataAnnotations;

namespace ArchViz.Api.Contracts.Scenes;

public record SubmitSceneAssessmentRequest(
    [Range(1, int.MaxValue)] int RequirementsRevisionId,
    [Range(1, int.MaxValue)] int ViewsCount,
    [Range(1, 5)] int ModelingComplexity,
    [Required]
    [Range(typeof(decimal), "0", "1")] decimal? CustomModelingRatio,
    [Range(1, 5)] int MaterialsComplexity,
    [Range(1, 5)] int LightingComplexity,
    [Range(1, 5)] int PostProcessingComplexity);