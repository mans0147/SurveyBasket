namespace SurveyBasket.API.Contracts.Votes;

public record VoteResultResponse(
    int PollId,
    string PollTitle,
    int TotalVotes,
    IReadOnlyCollection<QuestionResultResponse> Questions);

public record QuestionResultResponse(
    int QuestionId,
    string Question,
    IReadOnlyCollection<AnswerResultResponse> Answers);

public record AnswerResultResponse(int AnswerId, string Answer, int Votes, double Percentage);
