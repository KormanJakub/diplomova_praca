using System.ComponentModel.DataAnnotations;

namespace nia_api.Requests;

public class NewPasswordRequest
{
    [Required, EmailAddress, StringLength(254)]
    public string? Email { get; set; }
    [Required, StringLength(128, MinimumLength = 32)]
    public string? Token { get; set; }
    [Required, StringLength(128, MinimumLength = 8)]
    public string? NewPassword { get; set; }
    [Required, StringLength(128, MinimumLength = 8)]
    public string? RepeatNewPassword { get; set; }
}
