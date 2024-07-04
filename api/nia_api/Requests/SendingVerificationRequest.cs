using System.ComponentModel.DataAnnotations;

namespace nia_api.Requests;

public class SendingVerificationRequest
{
    [Required, EmailAddress, Display(Name = "Registered email address")]
    public string Email { get; set; }
    public bool EmailSent { get; set; }
}