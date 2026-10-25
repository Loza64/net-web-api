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
        RuleFor(x => x.Sort)
            .Must(values => values is null || values.All(value =>
            {
                var parts = value.Split(',', StringSplitOptions.TrimEntries);
                return parts.Length is 1 or 2
                       && new[] { "id", "name" }.Contains(parts[0], StringComparer.OrdinalIgnoreCase)
                       && (parts.Length == 1 || new[] { "asc", "desc" }
                           .Contains(parts[1], StringComparer.OrdinalIgnoreCase));
            }))
            .WithMessage("El orden debe usar id o name y la dirección asc o desc.");
    }
}
