namespace ArchViz.Domain.Scenes;

public sealed class Scene
{
    public int Id { get; private set; }
    public int OrderId { get; private set; }
    public string Name { get; private set; }
    public SceneStatus Status { get; private set; }
    = SceneStatus.AwaitingAssessment;

    private Scene()
    {
        Name = string.Empty;
    }

    public Scene(int orderId, string name)
    {
        if (orderId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(orderId),
                "Order ID must be greater than zero.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var trimmedName = name.Trim();

        if (trimmedName.Length > 200)
        {
            throw new ArgumentException(
                "Scene name must not exceed 200 characters.",
                nameof(name));
        }

        OrderId = orderId;
        Name = trimmedName;
    }

    public void SubmitAssessment()
    {
        if (Status != SceneStatus.AwaitingAssessment)
        {
            throw new InvalidOperationException(
                "Only scenes awaiting assessment can be assessed.");
        }

        Status = SceneStatus.AwaitingPlanApproval;
    }

    public void ApprovePlan()
    {
        if (Status != SceneStatus.AwaitingPlanApproval)
        {
            throw new InvalidOperationException(
                "Only scenes awaiting plan approval can become ready.");
        }

        Status = SceneStatus.Ready;
    }

    public string? ClosedById { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }

    public void SubmitRender() => Move(SceneStatus.InProgress, SceneStatus.InternalReview);
    public void PublishRender() => Move(SceneStatus.InternalReview, SceneStatus.ClientReview);
    public void ReturnFromInternalReview() => Move(SceneStatus.InternalReview, SceneStatus.InProgress);
    public void AcceptRender() => Move(SceneStatus.ClientReview, SceneStatus.ClientAccepted);
    public void ReturnFromClientReview() => Move(SceneStatus.ClientReview, SceneStatus.InProgress);
    public void ReopenAssessment() => Move(SceneStatus.InProgress, SceneStatus.AwaitingAssessment);

    public void Close(string managerId)
    {
        Move(SceneStatus.ClientAccepted, SceneStatus.Closed);
        ClosedById = managerId;
        ClosedAt = DateTimeOffset.UtcNow;
    }

    private void Move(SceneStatus expected, SceneStatus next)
    {
        if (Status != expected)
        {
            throw new InvalidOperationException("The scene is not in the expected state.");
        }

        Status = next;
    }

    public void StartWork()
    {
        if (Status != SceneStatus.Ready)
        {
            throw new InvalidOperationException(
                "Only ready scenes can be started.");
        }

        Status = SceneStatus.InProgress;
    }
}