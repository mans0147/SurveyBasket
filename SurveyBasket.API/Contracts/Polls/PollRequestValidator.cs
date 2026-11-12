using FluentValidation;

namespace SurveyBasket.API.Contracts.Polls;

public class PollRequestValidator : AbstractValidator<PollRequest>
{
    public PollRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .Length(3, 100);

        RuleFor(x => x.Summary)
            .NotEmpty()
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .Length(3, 1500);

        RuleFor(x => x.StartsAt)
            .NotEmpty()
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow));

        RuleFor(x => x.EndsAt)
            .NotEmpty();

        RuleFor(x => x)
            .Must(HasValidDates)
            .WithName(nameof(PollRequest.EndsAt))
            .WithMessage("{PropertyName} must be greater than or equals start date");

        RuleForEach(request => request.Questions).SetValidator(new QuestionRequestValidator());
    }


    private bool HasValidDates(PollRequest pollRequest)
    {
        return pollRequest.EndsAt >= pollRequest.StartsAt;
    }
}
