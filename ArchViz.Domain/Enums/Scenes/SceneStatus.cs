namespace ArchViz.Domain.Scenes;

public enum SceneStatus
{
    AwaitingAssessment = 0,
    AwaitingPlanApproval = 1,
    Ready = 2,
    InProgress = 3,
    InternalReview = 4,
    ClientReview = 5,
    ClientAccepted = 6,
    Closed = 7
}