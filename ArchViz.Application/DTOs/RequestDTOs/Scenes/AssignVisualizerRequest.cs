using System.ComponentModel.DataAnnotations;

namespace ArchViz.Api.Contracts.Scenes;

public record AssignVisualizerRequest(
    [Required]
    string VisualizerId);