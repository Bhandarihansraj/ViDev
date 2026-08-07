using FluentValidation;
using ViDev.Api.Dtos;

namespace ViDev.Api.Validators;

public class CreateTemplateRequestValidator : AbstractValidator<CreateTemplateRequest>
{
    public CreateTemplateRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(256)
            .Matches(@"^[A-Za-z][A-Za-z0-9_/ -]*$");

        RuleFor(x => x.AstJson)
            .NotEmpty()
            .Must(x => !string.IsNullOrWhiteSpace(x));

        RuleFor(x => x.Description)
            .MaximumLength(1024);

        RuleFor(x => x.Tags)
            .Must(tags => tags.Count <= 10).WithMessage("Maximum 10 tags allowed.")
            .ForEach(tag => tag.MaximumLength(64));
    }
}
