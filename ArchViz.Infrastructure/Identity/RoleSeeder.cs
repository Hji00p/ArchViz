using Microsoft.AspNetCore.Identity;

namespace ArchViz.Infrastructure.Identity;

public static class RoleSeeder
{
    public static async Task SeedAsync(
        RoleManager<IdentityRole> roleManager)
    {
        string[] roles = ["Admin", "Manager", "Visualizer", "Client"];

        foreach (var role in roles)
        {
            if (await roleManager.RoleExistsAsync(role))
            {
                continue;
            }

            var result = await roleManager.CreateAsync(
                new IdentityRole(role));

            if (!result.Succeeded)
            {
                var errors = string.Join(
                    ", ",
                    result.Errors.Select(error => error.Description));

                throw new InvalidOperationException(errors);
            }
        }
    }
}