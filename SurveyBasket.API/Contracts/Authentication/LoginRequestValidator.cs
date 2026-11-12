namespace SurveyBasket.API.Contracts.Authentication;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .MaximumLength(256)
            .Must(value => string.IsNullOrEmpty(value) || !value.Contains('@') || new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(value))
            .WithMessage("Enter a valid email address or username.");

        RuleFor(x => x.Password)
            .NotEmpty();
    }
}
