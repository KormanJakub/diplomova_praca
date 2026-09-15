using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson;
using nia_api.Enums;
using nia_api.Services;

namespace nia_api.Models;

public class Order
{
    [BsonId(IdGenerator = typeof(NullIdChecker))]
    [BsonElement("_id"), BsonRepresentation(BsonType.Int64)]
    public int Id { get; set; }
    [BsonElement("customizations"), BsonRepresentation(BsonType.String)]
    public List<Guid> Customizations { get; set; } = new List<Guid>();

    [BsonElement("totalPrice"), BsonRepresentation(BsonType.Decimal128)]
    public decimal TotalPrice { get; set; }
    [BsonElement("userId"), BsonRepresentation(BsonType.String)]
    public Guid UserId { get; set; }
    
    [BsonElement("statusOrder"), BsonRepresentation(BsonType.String)]
    public EStatus StatusOrder { get; set; }
    [BsonElement("paymentId"), BsonRepresentation(BsonType.String)]
    public string PaymentId { get; set; }

    [BsonElement("paymentStatus"), BsonRepresentation(BsonType.String)]
    public string PaymentStatus { get; set; }
    [BsonElement("paymentMethod"), BsonRepresentation(BsonType.String)]
    public string PaymentMethod { get; set; } = "Stripe";
    [BsonElement("paymentFee"), BsonRepresentation(BsonType.Decimal128)]
    public decimal PaymentFee { get; set; } = 0.00m;
    [BsonElement("deliveryMethod"), BsonRepresentation(BsonType.String)]
    public string DeliveryMethod { get; set; } = "HomeDelivery";
    [BsonElement("deliveryFee"), BsonRepresentation(BsonType.Decimal128)]
    public decimal DeliveryFee { get; set; } = 0.00m;
    [BsonElement("packetaPointId"), BsonRepresentation(BsonType.String)]
    public string? PacketaPointId { get; set; }
    [BsonElement("packetaPointName"), BsonRepresentation(BsonType.String)]
    public string? PacketaPointName { get; set; }
    [BsonElement("packetaPointAddress"), BsonRepresentation(BsonType.String)]
    public string? PacketaPointAddress { get; set; }
    [BsonElement("cancellationToken"), BsonRepresentation(BsonType.String)]
    public string CancellationToken { get; set; }
    [BsonElement("cancellationTokenExpiresAt"), BsonRepresentation(BsonType.DateTime)]
    public DateTime? CancellationTokenExpiresAt { get; set; }
    [BsonElement("followToken"), BsonRepresentation(BsonType.String)]
    public string FollowToken { get; set; }
    [BsonElement("followTokenExpiresAt"), BsonRepresentation(BsonType.DateTime)]
    public DateTime? FollowTokenExpiresAt { get; set; }
    [BsonElement("createdAt"), BsonRepresentation(BsonType.DateTime)]
    public DateTime? CreatedAt { get; set; }
    [BsonElement("updatedAt"), BsonRepresentation(BsonType.DateTime)]
    public DateTime? UpdatedAt { get; set; } = LocalTimeService.LocalTime();

    [BsonElement("orderNumber"), BsonIgnoreIfNull]
    public string? OrderNumber { get; set; }

    [BsonElement("lines"), BsonIgnoreIfNull]
    public List<nia_api.Domain.Orders.OrderLineSnapshot>? Lines { get; set; }

    [BsonElement("customerSnapshot"), BsonIgnoreIfNull]
    public nia_api.Domain.Orders.OrderCustomerSnapshot? CustomerSnapshot { get; set; }

    [BsonElement("deliverySnapshot"), BsonIgnoreIfNull]
    public nia_api.Domain.Orders.OrderDeliverySnapshot? DeliverySnapshot { get; set; }

    [BsonElement("pricingSnapshot"), BsonIgnoreIfNull]
    public nia_api.Domain.Orders.OrderPricingSnapshot? PricingSnapshot { get; set; }

    [BsonElement("idempotencyKey"), BsonIgnoreIfNull]
    public string? IdempotencyKey { get; set; }
}
