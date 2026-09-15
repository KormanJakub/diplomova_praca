using System.ComponentModel.DataAnnotations;

namespace nia_api.Requests;

public class LoginRequest
{
    [Required, EmailAddress, StringLength(254)] public string? Email { get; set; }
    [Required, StringLength(128, MinimumLength = 6)] public string? Password { get; set; }
}
