using FluentValidation;
using ViDev.Api.Dtos;

namespace ViDev.Api.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .Length(3, 128)
            .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("Username can only contain alphanumeric characters and underscores.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .Length(8, 128);
    }
}
