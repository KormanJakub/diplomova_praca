using nia_api.Models;

namespace nia_api.Requests;

public class GuestCustomizationRequest
{
    public GuestData GuestData { get; set; }
    public List<CustomizationRequest> Customizations { get; set; }
}

public class GuestData
{
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Country { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? Zip {  get; set; }
}