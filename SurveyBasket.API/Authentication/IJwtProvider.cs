namespace SurveyBasket.API.Authentication;

public interface IJwtProvider
{
    (string Token, int ExpiresIn) GenerateToken(ApplicationUser user, IEnumerable<string> roles);
}
