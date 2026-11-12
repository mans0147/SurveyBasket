namespace SurveyBasket.API.Contracts.Users;

public record CreateUserRequest(string FirstName, string LastName, string Email, string Password, string Role);
