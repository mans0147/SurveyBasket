
using Microsoft.AspNetCore.Identity;

namespace SurveyBasket.API.Services;

public class AuthService(UserManager<ApplicationUser> userManager) : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    public async Task<AuthResponse?> GetTokenAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        // Check if the user exists in the database
        var user = await _userManager.FindByEmailAsync(email);

        if(user is null)
            return null;

        // Check if the password is correct
        var isValidPassword = await _userManager.CheckPasswordAsync(user, password);

        if(!isValidPassword)
            return null;

        // Generate a JWT token


        // Return the token
        return new AuthResponse(user.Id, user.Email, user.FirstName, user.LastName, "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c", 3600);
    }
}
