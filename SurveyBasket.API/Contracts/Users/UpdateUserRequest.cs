namespace SurveyBasket.API.Contracts.Users;

public record UpdateUserRequest(string FirstName, string LastName, bool IsLocked, string Role);
