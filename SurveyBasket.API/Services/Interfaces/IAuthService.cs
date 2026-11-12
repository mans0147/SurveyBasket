namespace SurveyBasket.API.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponse?> GetTokenAsync(string emailOrUsername, string password, CancellationToken cancellationToken = default);
    Task<IdentityResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
}
