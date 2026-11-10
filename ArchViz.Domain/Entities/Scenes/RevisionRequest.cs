namespace ArchViz.Domain.Scenes;

public enum RevisionClassification
{
    Correction = 1,
    ScopeChange = 2
}

public sealed class RevisionRequest
{
    public int Id { get; private set; }
    public int RenderVersionId { get; private set; }
    public string RequestedById { get; private set; } = string.Empty;
    public string Comment { get; private set; } = string.Empty;
    public bool IsInternal { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public RevisionClassification? Classification { get; private set; }
    public string? ClassificationReason { get; private set; }
    public string? ClassifiedById { get; private set; }
    public DateTimeOffset? ClassifiedAt { get; private set; }
    public int? RequirementsRevisionId { get; private set; }

    private RevisionRequest() { }

    public RevisionRequest(int renderVersionId, string requestedById, string comment, bool isInternal)
    {
        RenderVersionId = renderVersionId;
        RequestedById = requestedById;
        Comment = comment.Trim();
        IsInternal = isInternal;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void Classify(RevisionClassification classification, string managerId,
        string reason, int? requirementsRevisionId)
    {
        if (IsInternal || Classification is not null)
        {
            throw new InvalidOperationException("The request cannot be classified.");
        }

        Classification = classification;
        ClassificationReason = reason.Trim();
        ClassifiedById = managerId;
        ClassifiedAt = DateTimeOffset.UtcNow;
        RequirementsRevisionId = requirementsRevisionId;
    }
}
