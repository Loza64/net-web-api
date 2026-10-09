namespace Module.Application.Contracts.Profile;

public record ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmNewPassword);
