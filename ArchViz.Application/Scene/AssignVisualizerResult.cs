namespace ArchViz.Application.Scenes;

public enum AssignVisualizerStatus
{
    Success,
    NotFound,
    InvalidState,
    InvalidVisualizer,
    AlreadyAssigned
}

public record AssignVisualizerResult(
    AssignVisualizerStatus Status,
    int? AssignmentId = null);