using FluentValidation;
using ViDev.Api.Dtos;

namespace ViDev.Api.Validators;

public class GenerateRequestValidator : AbstractValidator<GenerateRequest>
{
    public GenerateRequestValidator()
    {
        RuleFor(x => x.AstJson)
            .NotEmpty().WithMessage("AST JSON is required.");

        RuleFor(x => x.ProjectName)
            .NotEmpty().WithMessage("Project name is required.")
            .Length(1, 128).WithMessage("Project name must be between 1 and 128 characters.")
            .Matches("^[A-Za-z][A-Za-z0-9_.]*$").WithMessage("Project name must start with a letter and contain only alphanumeric characters, underscores, and dots.");
    }
}
