namespace SurveyBasket.API.Authentication;

public interface IJwtProvider
{
    (string token, int exPiresIn) GenerateToken(ApplicationUser user);
}
