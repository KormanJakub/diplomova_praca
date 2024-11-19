namespace nia_api.Requests;

public class OrderRequest
{
    public List<string> CustomizationId { get; set; }
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Country { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? Zip { get; set; }
}