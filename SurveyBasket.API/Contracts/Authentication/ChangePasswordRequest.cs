namespace SurveyBasket.API.Contracts.Authentication;

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
