namespace SurveyBasket.API.Contracts.Users;

public record UserResponse(string Id, string FirstName, string LastName, string Email, bool IsLocked, IReadOnlyCollection<string> Roles);
