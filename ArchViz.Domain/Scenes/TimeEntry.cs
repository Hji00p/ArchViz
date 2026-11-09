namespace ArchViz.Domain.Scenes;

public sealed class TimeEntry
{
    public int Id { get; private set; }
    public int SceneId { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public DateOnly WorkDate { get; private set; }
    public decimal Hours { get; private set; }
    public string Description { get; private set; } = string.Empty;

    private TimeEntry() { }

    public TimeEntry(int sceneId, string userId, DateOnly workDate, decimal hours, string description)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sceneId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        SceneId = sceneId;
        UserId = userId;
        Revise(workDate, hours, description);
    }

    public void Revise(DateOnly workDate, decimal hours, string description)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(hours);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        WorkDate = workDate;
        Hours = hours;
        Description = description.Trim();
    }
}
