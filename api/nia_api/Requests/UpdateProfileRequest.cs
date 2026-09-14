namespace nia_api.Requests;

public sealed class UpdateProfileRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Country { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? Zip { get; set; }
}
