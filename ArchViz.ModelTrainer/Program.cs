using Microsoft.ML;
using Microsoft.ML.Data;

var dataPath = Path.Combine(
    AppContext.BaseDirectory,
    "Data",
    "ArchViz_Synthetic_Training_Data.csv");

if (!File.Exists(dataPath))
{
    Console.Error.WriteLine($"CSV file not found: {dataPath}");
    return;
}

var mlContext = new MLContext(seed: 42);

var data = mlContext.Data.LoadFromTextFile<SceneTrainingRow>(
    dataPath,
    hasHeader: true,
    separatorChar: ',');

var rows = mlContext.Data
    .CreateEnumerable<SceneTrainingRow>(data, reuseRowObject: false)
    .ToList();

Console.WriteLine($"Total rows: {rows.Count}");

foreach (var group in rows.GroupBy(row => row.Split).OrderBy(group => group.Key))
{
    Console.WriteLine($"{group.Key}: {group.Count()}");
}

var trainRows = rows.Where(row => row.Split == "Train").ToArray();
var validationRows = rows.Where(row => row.Split == "Validation").ToArray();

var trainData = mlContext.Data.LoadFromEnumerable(trainRows);
var validationData = mlContext.Data.LoadFromEnumerable(validationRows);

var pipeline = mlContext.Transforms.Concatenate(
        "Features",
        nameof(SceneTrainingRow.ViewsCount),
        nameof(SceneTrainingRow.ModelingComplexity),
        nameof(SceneTrainingRow.CustomModelingRatio),
        nameof(SceneTrainingRow.MaterialsComplexity),
        nameof(SceneTrainingRow.LightingComplexity),
        nameof(SceneTrainingRow.PostProcessingComplexity))
    .Append(mlContext.Transforms.NormalizeMinMax("Features"))
    .Append(mlContext.Regression.Trainers.Sdca(
        labelColumnName: nameof(SceneTrainingRow.Hours)));

var model = pipeline.Fit(trainData);
var predictions = model.Transform(validationData);

var metrics = mlContext.Regression.Evaluate(
    predictions,
    labelColumnName: nameof(SceneTrainingRow.Hours));

var averageTrainHours = trainRows.Average(row => row.Hours);
var baselineMae = validationRows.Average(
    row => Math.Abs(row.Hours - averageTrainHours));

Console.WriteLine($"Baseline validation MAE: {baselineMae:F2} hours");
Console.WriteLine($"Model validation MAE: {metrics.MeanAbsoluteError:F2} hours");
Console.WriteLine($"Model validation RMSE: {metrics.RootMeanSquaredError:F2} hours");

var testRows = rows.Where(row => row.Split == "Test").ToArray();
var testData = mlContext.Data.LoadFromEnumerable(testRows);

var testPredictions = model.Transform(testData);
var testMetrics = mlContext.Regression.Evaluate(
    testPredictions,
    labelColumnName: nameof(SceneTrainingRow.Hours));

var testBaselineMae = testRows.Average(
    row => Math.Abs(row.Hours - averageTrainHours));

Console.WriteLine($"Baseline test MAE: {testBaselineMae:F2} hours");
Console.WriteLine($"Model test MAE: {testMetrics.MeanAbsoluteError:F2} hours");
Console.WriteLine($"Model test RMSE: {testMetrics.RootMeanSquaredError:F2} hours");

var modelPath = Path.Combine(
    AppContext.BaseDirectory,
    "ArchVizHoursModel.zip");

mlContext.Model.Save(model, trainData.Schema, modelPath);
Console.WriteLine($"Model saved to: {modelPath}");

var savedModel = mlContext.Model.Load(modelPath, out _);

var predictor = mlContext.Model
    .CreatePredictionEngine<SceneTrainingRow, SceneHoursPrediction>(
        savedModel);

var prediction = predictor.Predict(new SceneTrainingRow
{
    ViewsCount = 2,
    ModelingComplexity = 3,
    CustomModelingRatio = 0.5f,
    MaterialsComplexity = 2,
    LightingComplexity = 3,
    PostProcessingComplexity = 2
});

Console.WriteLine($"Test Kitchen forecast: {prediction.Hours:F2} hours");

public sealed class SceneTrainingRow
{
    [LoadColumn(0)] public float ViewsCount { get; set; }
    [LoadColumn(1)] public float ModelingComplexity { get; set; }
    [LoadColumn(2)] public float CustomModelingRatio { get; set; }
    [LoadColumn(3)] public float MaterialsComplexity { get; set; }
    [LoadColumn(4)] public float LightingComplexity { get; set; }
    [LoadColumn(5)] public float PostProcessingComplexity { get; set; }
    [LoadColumn(6)] public float Hours { get; set; }
    [LoadColumn(7)] public string Split { get; set; } = string.Empty;
}

public sealed class SceneHoursPrediction
{
    [ColumnName("Score")]
    public float Hours { get; set; }
}