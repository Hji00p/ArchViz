namespace ArchViz.Application.Users;

public interface IUserDirectory
{
    Task<bool> IsInRoleAsync(string userId, string role);
}