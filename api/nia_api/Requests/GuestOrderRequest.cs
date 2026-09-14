using nia_api.Models;

namespace nia_api.Requests;

public class GuestOrderRequest
{
    public string GuestUserId { get; set; }
    public List<Guid> CustomizationsId { get; set; }
    public string? PaymentMethod { get; set; }
    public string? DeliveryMethod { get; set; }
    public string? PacketaPointId { get; set; }
    public string? PacketaPointName { get; set; }
    public string? PacketaPointAddress { get; set; }
}