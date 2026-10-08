using FluentValidation;
using Module.Application.Contracts.Auth;

namespace Module.Application.Validators.Auth;

public class LogoutRequestValidator : AbstractValidator<LogoutRequest>
{
    public LogoutRequestValidator() => RuleFor(x => x.RefreshToken).NotEmpty();
}
