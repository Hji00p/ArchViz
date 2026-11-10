namespace ArchViz.Domain.Scenes;

public sealed class TimeEntryChange
{
    public int Id { get; private set; }
    public int TimeEntryId { get; private set; }
    public DateOnly PreviousWorkDate { get; private set; }
    public decimal PreviousHours { get; private set; }
    public string PreviousDescription { get; private set; } = string.Empty;
    public DateTimeOffset ChangedAt { get; private set; }

    private TimeEntryChange() { }

    public TimeEntryChange(TimeEntry entry)
    {
        TimeEntryId = entry.Id;
        PreviousWorkDate = entry.WorkDate;
        PreviousHours = entry.Hours;
        PreviousDescription = entry.Description;
        ChangedAt = DateTimeOffset.UtcNow;
    }
}
