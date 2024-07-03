using System.ComponentModel.DataAnnotations;

namespace nia_api.Requests;

public class RegisterRequest
{
    [Required]
    [EmailAddress]
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    [Required]
    public string? Password { get; set; }
    [Required]
    public string? RepeatPassword { get; set; }
}