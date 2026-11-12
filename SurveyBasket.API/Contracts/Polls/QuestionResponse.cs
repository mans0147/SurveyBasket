namespace SurveyBasket.API.Contracts.Polls;

public record QuestionResponse(int Id, string Content, IReadOnlyCollection<AnswerResponse> Answers);
