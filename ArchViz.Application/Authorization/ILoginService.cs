namespace ArchViz.Application.Auth;

public interface ILoginService
{
    Task<AccessToken?> LoginAsync(string email, string password);
}