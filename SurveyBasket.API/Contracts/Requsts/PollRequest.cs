namespace SurveyBasket.API.Contracts.Requstes;

public record PollRequest(
    string Title,
    string Summary,
    bool IsPublished,
    DateOnly StartsAt,
    DateOnly EndsAt
    );

