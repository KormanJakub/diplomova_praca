using System.ComponentModel.DataAnnotations;

namespace nia_api.Requests;

public class GuestOrderRequest
{
    [Required, StringLength(36)] public string GuestUserId { get; set; } = string.Empty;
    [Required, MinLength(1), MaxLength(25)] public List<Guid> CustomizationsId { get; set; } = [];
    [Required, RegularExpression("^(Stripe|IBAN|Dobierka)$")] public string? PaymentMethod { get; set; }
    [Required, RegularExpression("^(HomeDelivery|Packeta)$")] public string? DeliveryMethod { get; set; }
    [StringLength(100)] public string? PacketaPointId { get; set; }
    [StringLength(200)] public string? PacketaPointName { get; set; }
    [StringLength(300)] public string? PacketaPointAddress { get; set; }
    [StringLength(100)] public string? IdempotencyKey { get; set; }
}
