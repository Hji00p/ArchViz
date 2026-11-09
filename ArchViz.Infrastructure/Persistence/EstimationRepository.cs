using ArchViz.Application.Estimation;
using ArchViz.Domain.Estimation;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ArchViz.Infrastructure.Persistence;

public sealed class EstimationRepository : IEstimationRepository
{
    private readonly ArchVizDbContext _db;

    public EstimationRepository(ArchVizDbContext db)
    {
        _db = db;
    }

    public Task<MLModelVersion?> GetModelVersionAsync(
        string version,
        CancellationToken cancellationToken)
    {
        return _db.MLModelVersions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                model => model.Version == version,
                cancellationToken);
    }

    public Task<MLPrediction?> GetPredictionAsync(
    int assessmentId,
    int modelVersionId,
    CancellationToken cancellationToken)
    {
        return _db.MLPredictions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                prediction =>
                    prediction.AssessmentId == assessmentId &&
                    prediction.ModelVersionId == modelVersionId,
                cancellationToken);
    }

    public async Task<MLPrediction> AddPredictionAsync(
    MLPrediction prediction,
    CancellationToken cancellationToken)
    {
        _db.MLPredictions.Add(prediction);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return prediction;
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException postgres &&
                  postgres.SqlState == PostgresErrorCodes.UniqueViolation &&
                  postgres.ConstraintName ==
                      "IX_MLPredictions_AssessmentId_ModelVersionId")
        {
            _db.Entry(prediction).State = EntityState.Detached;

            var existing = await GetPredictionAsync(
                prediction.AssessmentId,
                prediction.ModelVersionId,
                cancellationToken);

            if (existing is null)
            {
                throw;
            }

            return existing;
        }
    }

    public Task<MLPrediction?> GetPredictionByIdAsync(
    int predictionId,
    CancellationToken cancellationToken)
    {
        return _db.MLPredictions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                prediction => prediction.Id == predictionId,
                cancellationToken);
    }

    

}