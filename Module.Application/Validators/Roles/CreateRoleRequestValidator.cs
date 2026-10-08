using FluentValidation;
using Module.Application.Contracts.Roles;

namespace Module.Application.Validators.Roles;

public class CreateRoleRequestValidator : AbstractValidator<CreateRoleRequest>
{
    public CreateRoleRequestValidator() => RuleFor(x => x.Name).NotEmpty().Length(2, 50);
}
