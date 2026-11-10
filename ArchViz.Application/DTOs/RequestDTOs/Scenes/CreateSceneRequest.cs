using System.ComponentModel.DataAnnotations;

namespace ArchViz.Api.Contracts.Scenes;

public record CreateSceneRequest(
    [Required]
    [StringLength(200)]
    string Name);