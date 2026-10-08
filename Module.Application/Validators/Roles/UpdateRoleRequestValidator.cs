using FluentValidation;
using Module.Application.Contracts.Roles;

namespace Module.Application.Validators.Roles;

public class UpdateRoleRequestValidator : AbstractValidator<UpdateRoleRequest>
{
    public UpdateRoleRequestValidator() => RuleFor(x => x.Name).NotEmpty().Length(2, 50);
}
