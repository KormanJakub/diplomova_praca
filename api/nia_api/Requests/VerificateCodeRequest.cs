using System.ComponentModel.DataAnnotations;

namespace nia_api.Requests;

public class VerificateCodeRequest
{
    [Required, EmailAddress]
    public string? Email { get; set; }
    public int VerificationCode { get; set; }
}