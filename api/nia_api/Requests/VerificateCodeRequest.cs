using System.ComponentModel.DataAnnotations;

namespace nia_api.Requests;

public class VerificateCodeRequest
{
    [Required, EmailAddress, StringLength(254)]
    public string? Email { get; set; }
    [Range(100000, 999999)] public int VerificationCode { get; set; }
}
