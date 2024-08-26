using System.ComponentModel.DataAnnotations;

namespace nia_api.Requests;

public class EmailRequest
{
    [Required, EmailAddress]
    public string? Email { get; set; }
}