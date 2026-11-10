namespace ArchViz.Application.Auth;

public interface IRegistrationService
{
    Task<RegistrationResult> RegisterAsync(
        string email,
        string password);
}