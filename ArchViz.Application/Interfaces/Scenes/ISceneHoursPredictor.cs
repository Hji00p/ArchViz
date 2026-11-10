using ArchViz.Domain.Scenes;

namespace ArchViz.Application.Scenes;

public interface ISceneHoursPredictor
{
    decimal PredictHours(SceneAssessment assessment);
}