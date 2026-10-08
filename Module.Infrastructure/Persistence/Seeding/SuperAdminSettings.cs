using System.ComponentModel.DataAnnotations;

namespace Module.Infrastructure.Persistence.Seeding;

public sealed class SuperAdminSettings
{
    public const string SectionName = "SuperAdmin";

    [Required, EmailAddress, MaxLength(255)]
    public string Email { get; set; } = default!;

    [Required, RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,72}$",
        ErrorMessage = "La contraseña debe tener 8-72 caracteres, con mayúscula, minúscula y número.")]
    public string Password { get; set; } = default!;

    [Required, RegularExpression("^[a-zA-Z0-9_.-]{3,50}$",
        ErrorMessage = "El username debe tener 3-50 caracteres (letras, números, _ . -).")]
    public string Username { get; set; } = "superadmin";

    [Required, MaxLength(100)]
    public string Name { get; set; } = "Super";

    [Required, MaxLength(100)]
    public string Surname { get; set; } = "Admin";
}
