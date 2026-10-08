using FluentValidation;
using Module.Application.Contracts.Permissions;

namespace Module.Application.Validators.Permissions;

public class PermissionListQueryValidator : AbstractValidator<PermissionListQuery>
{
    public PermissionListQueryValidator()
    {
        RuleFor(x => x.Page).Page();
        RuleFor(x => x.PageSize).PageSize();
        RuleFor(x => x.Search).Search();
        RuleFor(x => x.SortBy)
            .Must(value => string.IsNullOrWhiteSpace(value)
                           || new[] { "name", "title" }.Contains(value, StringComparer.OrdinalIgnoreCase))
            .WithMessage("El orden debe ser name o title.");
    }
}
