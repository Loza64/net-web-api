using FluentValidation;

namespace Module.Application.Validators;

internal static class Rules
{
    public static IRuleBuilderOptions<T, string> Username<T>(this IRuleBuilder<T, string> r) =>
        r.NotEmpty().Length(3, 50).Matches("^[a-zA-Z0-9_.-]+$");

    public static IRuleBuilderOptions<T, string> Email<T>(this IRuleBuilder<T, string> r) =>
        r.NotEmpty().EmailAddress().MaximumLength(255);

    public static IRuleBuilderOptions<T, string> StrongPassword<T>(this IRuleBuilder<T, string> r) =>
        r.NotEmpty().MinimumLength(8).MaximumLength(72)
            .Matches("[A-Z]").WithMessage("Debe contener una mayúscula.")
            .Matches("[a-z]").WithMessage("Debe contener una minúscula.")
            .Matches("[0-9]").WithMessage("Debe contener un número.");

    public static IRuleBuilderOptions<T, int> Page<T>(this IRuleBuilder<T, int> r) =>
        r.GreaterThanOrEqualTo(1);

    public static IRuleBuilderOptions<T, int> PageSize<T>(this IRuleBuilder<T, int> r) =>
        r.InclusiveBetween(1, 100);

    public static IRuleBuilderOptions<T, string?> Search<T>(this IRuleBuilder<T, string?> r) =>
        r.MaximumLength(100);
}
