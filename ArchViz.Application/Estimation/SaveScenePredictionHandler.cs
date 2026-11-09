using ArchViz.Application.Scenes;
using ArchViz.Domain.Estimation;

namespace ArchViz.Application.Estimation;

public sealed class SaveScenePredictionHandler
{
    private const string ModelVersion = "synthetic-sdca-v1";

    private readonly GetSceneAssessmentHandler _getAssessment;
    private readonly IEstimationRepository _estimation;

    public SaveScenePredictionHandler(
        GetSceneAssessmentHandler getAssessment,
        IEstimationRepository estimation)
    {
        _getAssessment = getAssessment;
        _estimation = estimation;
    }

    public async Task<SaveScenePredictionResult> HandleAsync(
        int orderId,
        int sceneId,
        string managerId,
        CancellationToken cancellationToken)
    {
        var assessment = await _getAssessment.HandleAsync(
            orderId, sceneId, managerId, cancellationToken);

        if (assessment is null)
        {
            return new(SaveScenePredictionStatus.AssessmentNotFound);
        }

        var model = await _estimation.GetModelVersionAsync(
            ModelVersion, cancellationToken);

        if (model is null)
        {
            return new(SaveScenePredictionStatus.ModelNotRegistered);
        }

        var prediction = await _estimation.GetPredictionAsync(
            assessment.Id, model.Id, cancellationToken);

        if (prediction is null)
        {
            prediction = await _estimation.AddPredictionAsync(
                new MLPrediction(
                    assessment.Id,
                    model.Id,
                    assessment.PredictedHours),
                cancellationToken);
        }

        return new(
            SaveScenePredictionStatus.Success,
            prediction.Id,
            prediction.PredictedHours);
    }
}