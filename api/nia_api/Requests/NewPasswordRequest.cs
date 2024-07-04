using System.ComponentModel.DataAnnotations;

namespace nia_api.Requests;

public class NewPasswordRequest
{
    [Required, EmailAddress]
    public string? Email { get; set; }
    [Required]
    public string? NewPassword { get; set; }
    [Required]
    public string? RepeatNewPassword { get; set; }
}