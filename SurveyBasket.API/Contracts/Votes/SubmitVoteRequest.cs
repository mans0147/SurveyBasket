namespace SurveyBasket.API.Contracts.Votes;

public record SubmitVoteRequest(IReadOnlyCollection<int> AnswerIds);
