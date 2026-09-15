using System.ComponentModel.DataAnnotations;

namespace nia_api.Requests;

public class EmailRequest
{
    [Required, EmailAddress, StringLength(254)]
    public string? Email { get; set; }
}
