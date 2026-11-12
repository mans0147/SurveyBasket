
using Microsoft.AspNetCore.Identity;
using SurveyBasket.API.Authentication;
using SurveyBasket.API.Authorization;

namespace SurveyBasket.API.Services;

public class AuthService(UserManager<ApplicationUser> userManager, IJwtProvider jwtProvider) : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly IJwtProvider _jwtProvider = jwtProvider;

    public async Task<AuthResponse?> GetTokenAsync(string emailOrUsername, string password, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(emailOrUsername) ??
            await _userManager.FindByNameAsync(emailOrUsername);

        if (user is null || !user.EmailConfirmed)
            return null;

        if (await _userManager.IsLockedOutAsync(user))
            return null;

        var isValidPassword = await _userManager.CheckPasswordAsync(user, password);

        if(!isValidPassword)
        {
            await _userManager.AccessFailedAsync(user);
            return null;
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        var (token, expiresIn) = _jwtProvider.GenerateToken(user, roles);

        return new AuthResponse(user.Id, user.Email!, user.FirstName, user.LastName, token, expiresIn);
    }

    public async Task<IdentityResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var user = new ApplicationUser
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email.Trim(),
            UserName = request.Email.Trim(),
            LockoutEnabled = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return result;

        var roleResult = await _userManager.AddToRoleAsync(user, SurveyBasketRoles.User);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            return roleResult;
        }

        return IdentityResult.Success;
    }
}
