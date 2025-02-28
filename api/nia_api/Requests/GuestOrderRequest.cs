using nia_api.Models;

namespace nia_api.Requests;

public class GuestOrderRequest
{
    public string GuestUserId { get; set; }
    public List<Guid> CustomizationsId { get; set; }
}