
using Microsoft.AspNetCore.Identity;
using SurveyBasket.API.Authentication;

namespace SurveyBasket.API.Services;

public class AuthService(UserManager<ApplicationUser> userManager, IJwtProvider jwtProvider) : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly IJwtProvider _jwtProvider = jwtProvider;

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

        var (token, expiresIn) = _jwtProvider.GenerateToken(user);

        // Return the token
        return new AuthResponse(user.Id, user.Email, user.FirstName, user.LastName,token, expiresIn);
    }
}
