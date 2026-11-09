namespace ArchViz.Domain.Estimation;

public sealed class MLModelVersion
{
    public int Id { get; private set; }
    public string Version { get; private set; }
    public DateTimeOffset TrainedAt { get; private set; }
    public double Mae { get; private set; }
    public double Rmse { get; private set; }
    public string ModelPath { get; private set; }

    private MLModelVersion()
    {
        Version = string.Empty;
        ModelPath = string.Empty;
    }

    public MLModelVersion(
        string version,
        DateTimeOffset trainedAt,
        double mae,
        double rmse,
        string modelPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelPath);

        Version = version.Trim();
        TrainedAt = trainedAt.ToUniversalTime();
        Mae = mae;
        Rmse = rmse;
        ModelPath = modelPath.Trim();
    }
}