namespace SurveyBasket.API.Contracts.Polls;

public record QuestionRequest(string Content, IReadOnlyCollection<string> Answers);
