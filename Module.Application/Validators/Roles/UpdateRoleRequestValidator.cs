using FluentValidation;
using Module.Application.Contracts.Roles;

namespace Module.Application.Validators.Roles;

public class UpdateRoleRequestValidator : AbstractValidator<UpdateRoleRequest>
{
    public UpdateRoleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().Length(2, 50);
        RuleFor(x => x.Permissions)
            .Must(permissions => permissions is null || permissions.Count <= 500)
            .WithMessage("La lista de permisos no puede exceder 500 elementos.");
        RuleForEach(x => x.Permissions).ChildRules(permission =>
            permission.RuleFor(item => item.Id).GreaterThan(0));
    }
}
