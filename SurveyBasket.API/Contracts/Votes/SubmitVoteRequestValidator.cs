namespace SurveyBasket.API.Contracts.Votes;

public class SubmitVoteRequestValidator : AbstractValidator<SubmitVoteRequest>
{
    public SubmitVoteRequestValidator()
    {
        RuleFor(request => request.AnswerIds).NotNull()
            .Must(answerIds => answerIds is { Count: > 0 and <= 100 })
            .WithMessage("Select an answer for each question in the poll.");
    }
}
