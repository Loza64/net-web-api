using FluentValidation;
using Module.Application.Contracts.Auth;

namespace Module.Application.Validators.Auth;

public class RefreshRequestValidator : AbstractValidator<RefreshRequest>
{
    public RefreshRequestValidator() => RuleFor(x => x.RefreshToken).NotEmpty();
}
