using Microsoft.AspNetCore.Identity;
using SurveyBasket.API.Authorization;

namespace SurveyBasket.API.Persistence;

public static class IdentitySeeder
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var roleName in new[] { SurveyBasketRoles.Admin, SurveyBasketRoles.User })
        {
            if (await roleManager.RoleExistsAsync(roleName))
                continue;

            var roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
            if (!roleResult.Succeeded)
                throw new InvalidOperationException($"Could not create the '{roleName}' role: {string.Join("; ", roleResult.Errors.Select(error => error.Description))}");
        }

        var email = configuration["Admin:Email"];
        var password = configuration["Admin:Password"];
        var username = string.IsNullOrWhiteSpace(configuration["Admin:Username"])
            ? "admin"
            : configuration["Admin:Username"]!;
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Configure Admin:Email and Admin:Password in User Secrets before starting the API.");

        var admin = await userManager.FindByEmailAsync(email);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                FirstName = configuration["Admin:FirstName"] ?? "Survey",
                LastName = configuration["Admin:LastName"] ?? "Administrator",
                Email = email,
                UserName = username,
                EmailConfirmed = true,
                LockoutEnabled = true
            };

            var userResult = await userManager.CreateAsync(admin, password);
            if (!userResult.Succeeded)
                throw new InvalidOperationException($"Could not create the administrator: {string.Join("; ", userResult.Errors.Select(error => error.Description))}");
        }
        else if (!string.Equals(admin.UserName, username, StringComparison.Ordinal))
        {
            var usernameOwner = await userManager.FindByNameAsync(username);
            if (usernameOwner is not null && usernameOwner.Id != admin.Id)
                throw new InvalidOperationException("The configured administrator username is already assigned to another account.");

            admin.UserName = username;
            var updateResult = await userManager.UpdateAsync(admin);
            if (!updateResult.Succeeded)
                throw new InvalidOperationException($"Could not update the administrator username: {string.Join("; ", updateResult.Errors.Select(error => error.Description))}");
        }

        if (!await userManager.IsInRoleAsync(admin, SurveyBasketRoles.Admin))
        {
            var assignmentResult = await userManager.AddToRoleAsync(admin, SurveyBasketRoles.Admin);
            if (!assignmentResult.Succeeded)
                throw new InvalidOperationException($"Could not assign the administrator role: {string.Join("; ", assignmentResult.Errors.Select(error => error.Description))}");
        }
    }
}
