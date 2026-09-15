using System.ComponentModel.DataAnnotations;

namespace nia_api.Requests;

public class RegisterRequest
{
    [Required, EmailAddress, StringLength(254)] public string? Email { get; set; }
    [Required, StringLength(100)] public string? FirstName { get; set; }
    [Required, StringLength(100)] public string? LastName { get; set; }
    [Required, StringLength(128, MinimumLength = 8)] public string? Password { get; set; }
    [Required, StringLength(128, MinimumLength = 8)] public string? RepeatPassword { get; set; }
}
