using ArchViz.Domain.Estimation;

namespace ArchViz.Application.Estimation;

public interface IEstimationRepository
{
    Task<MLModelVersion?> GetModelVersionAsync(
        string version,
        CancellationToken cancellationToken);

    Task<MLPrediction?> GetPredictionAsync(
        int assessmentId,
        int modelVersionId,
        CancellationToken cancellationToken);

    Task<MLPrediction> AddPredictionAsync(
        MLPrediction prediction,
        CancellationToken cancellationToken);

    Task<MLPrediction?> GetPredictionByIdAsync(
    int predictionId,
    CancellationToken cancellationToken);
}
