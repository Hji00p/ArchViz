using ArchViz.Application.Auth;
using Microsoft.AspNetCore.Identity;

namespace ArchViz.Infrastructure.Identity;

public sealed class LoginService : ILoginService
{
    private readonly UserManager<AppUser> _users;
    private readonly SignInManager<AppUser> _signIn;
    private readonly IAccessTokenService _tokens;

    public LoginService(
        UserManager<AppUser> users,
        SignInManager<AppUser> signIn,
        IAccessTokenService tokens)
    {
        _users = users;
        _signIn = signIn;
        _tokens = tokens;
    }

    public async Task<AccessToken?> LoginAsync(
        string email,
        string password)
    {
        var user = await _users.FindByEmailAsync(email.Trim());

        if (user is null)
        {
            return null;
        }

        var result = await _signIn.CheckPasswordSignInAsync(
            user,
            password,
            lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            return null;
        }

        var roles = await _users.GetRolesAsync(user);

        return _tokens.Create(
            user.Id,
            user.Email!,
            roles.ToArray());
    }
}