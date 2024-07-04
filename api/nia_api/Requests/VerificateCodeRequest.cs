using System.ComponentModel.DataAnnotations;

namespace nia_api.Requests;

public class ForgotPasswordRequest
{
    [Required, EmailAddress]
    public string? Email { get; set; }
    public int VerificationCode { get; set; }
}