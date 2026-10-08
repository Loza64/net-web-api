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
        RuleFor(x => x.RoleId).GreaterThan(0).When(x => x.RoleId.HasValue);
        RuleFor(x => x.SortBy)
            .Must(value => string.IsNullOrWhiteSpace(value)
                           || new[] { "username", "email", "name", "createdAt" }
                               .Contains(value, StringComparer.OrdinalIgnoreCase))
            .WithMessage("El orden debe ser username, email, name o createdAt.");
    }
}
