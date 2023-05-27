using FluentValidation;
using HttpServer.Models;

namespace HttpServer.Validators;

public class MessageValidator : AbstractValidator<Message>
{
    public MessageValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Message content in required.")
            .MaximumLength(1000).WithMessage("Message content must not exceed 1000 characters.");

        RuleFor(x => x.Timestamp)
            .LessThanOrEqualTo(DateTimeOffset.Now).WithMessage("Message timestamp must not be from the future.");

        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id cannot be empty.");
    }
}