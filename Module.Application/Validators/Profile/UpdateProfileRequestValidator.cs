using FluentValidation;
using Module.Application.Contracts.Profile;

namespace Module.Application.Validators.Profile;

public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.Username).Username();
        RuleFor(x => x.Email).Email();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Surname).NotEmpty().MaximumLength(100);

        RuleFor(x => x.Photo!)
            .MaximumLength(500)
            .Must(IsHttpUrl).WithMessage("La foto debe ser una URL http(s) válida.")
            .When(x => !string.IsNullOrWhiteSpace(x.Photo));
    }

    private static bool IsHttpUrl(string value) =>
        Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
