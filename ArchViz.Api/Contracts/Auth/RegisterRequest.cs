using System.ComponentModel.DataAnnotations;

namespace ArchViz.Api.Contracts.Auth;

public record RegisterRequest(
    [Required]
    [EmailAddress]
    string Email,

    [Required]
    string Password);