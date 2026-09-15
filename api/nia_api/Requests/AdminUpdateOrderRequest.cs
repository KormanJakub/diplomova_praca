using System.ComponentModel.DataAnnotations;
using nia_api.Enums;

namespace nia_api.Requests;

public sealed class AdminUpdateOrderRequest
{
    public EStatus? StatusOrder { get; set; }
    [StringLength(50)] public string? DeliveryMethod { get; set; }
    [StringLength(100)] public string? PacketaPointId { get; set; }
    [StringLength(200)] public string? PacketaPointName { get; set; }
    [StringLength(300)] public string? PacketaPointAddress { get; set; }
}
