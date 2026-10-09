using FluentValidation;
using Module.Application.Contracts.Permissions;

namespace Module.Application.Validators.Permissions;

public sealed class PermissionUpdateRequestValidator : AbstractValidator<PermissionUpdateRequest>
{
    public PermissionUpdateRequestValidator()
    {
        RuleFor(request => request.Title).NotEmpty().Length(3, 100);
    }
}
