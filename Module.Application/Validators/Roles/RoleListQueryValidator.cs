using FluentValidation;
using Module.Application.Contracts.Roles;

namespace Module.Application.Validators.Roles;

public class RoleListQueryValidator : AbstractValidator<RoleListQuery>
{
    public RoleListQueryValidator()
    {
        RuleFor(x => x.Page).Page();
        RuleFor(x => x.PageSize).PageSize();
        RuleFor(x => x.Search).Search();
        RuleFor(x => x.SortBy)
            .Must(value => string.IsNullOrWhiteSpace(value)
                           || new[] { "id", "name" }.Contains(value, StringComparer.OrdinalIgnoreCase))
            .WithMessage("El orden debe ser id o name.");
    }
}
