using ArchViz.Application.Users;
using Microsoft.AspNetCore.Identity;

namespace ArchViz.Infrastructure.Identity;

public sealed class UserDirectory : IUserDirectory
{
    private readonly UserManager<AppUser> _users;

    public UserDirectory(UserManager<AppUser> users)
    {
        _users = users;
    }

    public async Task<bool> IsInRoleAsync(
        string userId,
        string role)
    {
        var user = await _users.FindByIdAsync(userId);

        if (user is null)
        {
            return false;
        }

        return await _users.IsInRoleAsync(user, role);
    }
}