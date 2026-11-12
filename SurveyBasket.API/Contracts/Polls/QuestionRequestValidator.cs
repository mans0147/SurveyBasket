namespace SurveyBasket.API.Contracts.Polls;

public class QuestionRequestValidator : AbstractValidator<QuestionRequest>
{
    public QuestionRequestValidator()
    {
        RuleFor(question => question.Content).NotEmpty().Must(value => !string.IsNullOrWhiteSpace(value)).MaximumLength(500);
        RuleFor(question => question.Answers)
            .NotNull()
            .Must(answers => answers is { Count: >= 2 and <= 10 })
            .WithMessage("Each question must have between 2 and 10 answers.");
        RuleForEach(question => question.Answers)
            .NotEmpty()
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .MaximumLength(300);
        RuleFor(question => question.Answers)
            .Must(answers => answers is not null && answers.Select(answer => answer.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() == answers.Count)
            .WithMessage("Answer options must be unique.");
    }
}
