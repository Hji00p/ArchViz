namespace ArchViz.Application.Auth;

public interface IAccessTokenService
{
    AccessToken Create(
        string userId,
        string email,
        IReadOnlyCollection<string> roles);
}