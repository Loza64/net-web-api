using FluentValidation;
using Module.Application.Contracts.Roles;

namespace Module.Application.Validators.Roles;

public class SetRolePermissionsRequestValidator : AbstractValidator<SetRolePermissionsRequest>
{
    public SetRolePermissionsRequestValidator()
    {
        RuleFor(x => x.PermissionIds).Cascade(CascadeMode.Stop).NotNull()
            .Must(ids => ids is not null && ids.Count <= 500)
            .WithMessage("La lista es obligatoria y no puede exceder 500 permisos.");
        RuleForEach(x => x.PermissionIds).GreaterThan(0);
    }
}
