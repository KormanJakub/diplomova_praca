using System.ComponentModel.DataAnnotations;

namespace nia_api.Requests;

public class CustomizationRequest
{
    [StringLength(36)] public string? DesignId { get; set; }
    [Required, StringLength(36)] public string? ProductId { get; set; }
    [StringLength(500)] public string? UserDescription { get; set; }
    [Required, StringLength(80)] public string? ProductColorName { get; set; }
    [Required, StringLength(20)] public string? ProductSize { get; set; }
}
