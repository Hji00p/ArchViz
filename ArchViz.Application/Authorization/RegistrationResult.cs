namespace ArchViz.Application.Auth;

public record RegistrationResult(
    bool Succeeded,
    string? UserId,
    IReadOnlyList<string> Errors);