namespace ArchViz.Application.Auth;

public record AccessToken(
    string Token,
    DateTimeOffset ExpiresAt);