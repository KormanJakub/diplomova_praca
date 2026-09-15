using System.ComponentModel.DataAnnotations;

namespace nia_api.Requests;

public class GuestCustomizationRequest
{
    [Required] public GuestData GuestData { get; set; } = new();
    [Required, MinLength(1), MaxLength(25)] public List<CustomizationRequest> Customizations { get; set; } = [];
}

public class GuestData
{
    [Required, EmailAddress, StringLength(254)] public string? Email { get; set; }
    [Required, StringLength(100)] public string? FirstName { get; set; }
    [Required, StringLength(100)] public string? LastName { get; set; }
    [Required, StringLength(100)] public string? Country { get; set; }
    [Required, Phone, StringLength(30)] public string? PhoneNumber { get; set; }
    [Required, StringLength(250)] public string? Address { get; set; }
    [Required, StringLength(20)] public string? Zip { get; set; }
}
