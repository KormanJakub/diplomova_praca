using System.ComponentModel.DataAnnotations;

namespace nia_api.Requests;

public sealed class UpdateProfileRequest
{
    [StringLength(100)] public string? FirstName { get; set; }
    [StringLength(100)] public string? LastName { get; set; }
    [StringLength(100)] public string? Country { get; set; }
    [Phone, StringLength(30)] public string? PhoneNumber { get; set; }
    [StringLength(250)] public string? Address { get; set; }
    [StringLength(20)] public string? Zip { get; set; }
}
