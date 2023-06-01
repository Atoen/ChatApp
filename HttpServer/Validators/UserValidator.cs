using FluentValidation;
using HttpServer.Models;

namespace HttpServer.Validators;

public class UserValidator : AbstractValidator<UserDto>
{
    public UserValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username cannot be empty or whitespace only.")
            .MinimumLength(2).WithMessage("Username cannot be shorter than 2 characters.")
            .MaximumLength(32).WithMessage("Username cannot exceed 32 characters.");
    }
}