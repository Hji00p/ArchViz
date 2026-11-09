using ArchViz.Application.Scenes;
using ArchViz.Domain.Scenes;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace ArchViz.Infrastructure.MachineLearning;

public sealed class MlSceneHoursPredictor : ISceneHoursPredictor
{
    private readonly MLContext _mlContext = new();
    private readonly ITransformer _model;

    public MlSceneHoursPredictor()
    {
        var modelPath = Path.Combine(
            AppContext.BaseDirectory,
            "Models",
            "ArchVizHoursModel.zip");

        _model = _mlContext.Model.Load(modelPath, out _);
    }

    public decimal PredictHours(SceneAssessment assessment)
    {
        using var engine = _mlContext.Model
            .CreatePredictionEngine<ModelInput, ModelOutput>(_model);

        var prediction = engine.Predict(new ModelInput
        {
            ViewsCount = assessment.ViewsCount,
            ModelingComplexity = assessment.ModelingComplexity,
            CustomModelingRatio = (float)assessment.CustomModelingRatio,
            MaterialsComplexity = assessment.MaterialsComplexity,
            LightingComplexity = assessment.LightingComplexity,
            PostProcessingComplexity = assessment.PostProcessingComplexity
        });

        return Math.Round((decimal)prediction.Hours, 2);
    }

    public sealed class ModelInput
    {
        public float ViewsCount { get; set; }
        public float ModelingComplexity { get; set; }
        public float CustomModelingRatio { get; set; }
        public float MaterialsComplexity { get; set; }
        public float LightingComplexity { get; set; }
        public float PostProcessingComplexity { get; set; }
    }

    public sealed class ModelOutput
    {
        [ColumnName("Score")]
        public float Hours { get; set; }
    }
}