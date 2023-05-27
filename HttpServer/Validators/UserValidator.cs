using System.Text;
using FluentValidation;
using HttpServer.Models;

namespace HttpServer.Validators;

public class UsernameValidator : AbstractValidator<string>
{
    public UsernameValidator()
    {
        RuleFor(x => x)
            .NotEmpty().WithMessage("Username cannot be empty or whitespace only.")
            .MaximumLength(32).WithMessage("Username cannot exceed 32 characters.");
    }
}