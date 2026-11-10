using System.ComponentModel.DataAnnotations;

namespace ArchViz.Api.Contracts.Auth;

public record LoginRequest(
    [Required]
    [EmailAddress]
    string Email,

    [Required]
    string Password);