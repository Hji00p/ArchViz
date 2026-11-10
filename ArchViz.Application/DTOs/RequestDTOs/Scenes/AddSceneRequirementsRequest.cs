using System.ComponentModel.DataAnnotations;

namespace ArchViz.Api.Contracts.Scenes;

public record AddSceneRequirementsRequest(
    [Required]
    [StringLength(10000)]
    string Requirements);