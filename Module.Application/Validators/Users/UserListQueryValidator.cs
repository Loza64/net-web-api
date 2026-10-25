using FluentValidation;
using Module.Application.Contracts.Users;

namespace Module.Application.Validators.Users;

public class UserListQueryValidator : AbstractValidator<UserListQuery>
{
    public UserListQueryValidator()
    {
        RuleFor(x => x.Page).Page();
        RuleFor(x => x.PageSize).PageSize();
        RuleFor(x => x.Search).Search();
        RuleFor(x => x.Role).GreaterThan(0).When(x => x.Role.HasValue);
        RuleFor(x => x.Sort)
            .Must(values => values is null || values.All(value =>
            {
                var parts = value.Split(',', StringSplitOptions.TrimEntries);
                return parts.Length is 1 or 2
                       && new[] { "username", "email", "name", "createdAt" }
                           .Contains(parts[0], StringComparer.OrdinalIgnoreCase)
                       && (parts.Length == 1 || new[] { "asc", "desc" }
                           .Contains(parts[1], StringComparer.OrdinalIgnoreCase));
            }))
            .WithMessage("El orden debe usar username, email, name o createdAt y la dirección asc o desc.");
    }
}
