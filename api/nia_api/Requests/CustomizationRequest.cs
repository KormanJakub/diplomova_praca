using System.ComponentModel.DataAnnotations;

namespace nia_api.Requests;

public class CustomizationRequest
{
    public string? DesignId { get; set; }
    public string? ProductId { get; set; }
    public string? UserDescription { get; set; }
    public string? ProductColorName { get; set; }
    public string? ProductSize { get; set; }
}