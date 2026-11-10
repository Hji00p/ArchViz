namespace ArchViz.Application.Scenes;

public record TimeEntryInput(DateOnly WorkDate, decimal Hours, string Description);
public record TimeEntryResponse(
    int Id, string UserId, DateOnly WorkDate, decimal Hours, string Description);
public record SceneTimeSummary(
    decimal? ApprovedHours, decimal ActualHours, bool IsOverPlan,
    IReadOnlyList<TimeEntryResponse> Entries);

public enum TimeEntryStatus
{
    Success,
    NotFound,
    InvalidState
}

public record TimeEntryResult(TimeEntryStatus Status, TimeEntryResponse? Entry = null);

public interface ISceneTimeRepository
{
    Task<TimeEntryResult> SaveAsync(
        int sceneId, int? entryId, string userId, TimeEntryInput input,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<TimeEntryResponse>?> GetOwnAsync(
        int sceneId, string userId, CancellationToken cancellationToken);

    Task<SceneTimeSummary?> GetSummaryAsync(
        int orderId, int sceneId, string managerId, CancellationToken cancellationToken);
}
