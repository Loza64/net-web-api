using FluentValidation;
using Module.Application.Contracts.Users;

namespace Module.Application.Validators.Users;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Username).Username();
        RuleFor(x => x.Email).Email();
        RuleFor(x => x.Password).StrongPassword();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Surname).NotEmpty().MaximumLength(100);
        RuleFor(x => x.RoleId).GreaterThan(0);
    }
}
