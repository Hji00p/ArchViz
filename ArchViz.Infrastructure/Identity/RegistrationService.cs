using ArchViz.Application.Auth;
using ArchViz.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace ArchViz.Infrastructure.Identity;

public sealed class RegistrationService : IRegistrationService
{
    private readonly UserManager<AppUser> _users;
    private readonly ArchVizDbContext _db;

    public RegistrationService(
        UserManager<AppUser> users,
        ArchVizDbContext db)
    {
        _users = users;
        _db = db;
    }

    public async Task<RegistrationResult> RegisterAsync(
        string email,
        string password)
    {
        var user = new AppUser
        {
            UserName = email.Trim(),
            Email = email.Trim()
        };

        await using var transaction =
            await _db.Database.BeginTransactionAsync();

        var creationResult = await _users.CreateAsync(user, password);

        if (!creationResult.Succeeded)
        {
            return new RegistrationResult(
                false,
                null,
                creationResult.Errors
                    .Select(error => error.Description)
                    .ToArray());
        }

        var roleResult = await _users.AddToRoleAsync(user, "Client");

        if (!roleResult.Succeeded)
        {
            return new RegistrationResult(
                false,
                null,
                roleResult.Errors
                    .Select(error => error.Description)
                    .ToArray());
        }

        await transaction.CommitAsync();

        return new RegistrationResult(
            true,
            user.Id,
            Array.Empty<string>());
    }
}