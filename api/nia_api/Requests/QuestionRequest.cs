using System.ComponentModel.DataAnnotations;

namespace nia_api.Requests;

public class QuestionRequest
{
    [Required, StringLength(100)] public string? Name { get; set; }
    [Required, EmailAddress, StringLength(254)] public string? Email { get; set; }
    [Required, StringLength(2000)] public string? Description { get; set; }
    [StringLength(36)] public string? FileId { get; set; }
    [StringLength(300)] public string? PathOfUrl { get; set; }
}
