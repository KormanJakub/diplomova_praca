using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace nia_api.Domain.Orders;

public sealed class OrderLineSnapshot
{
    [BsonElement("customizationId"), BsonRepresentation(BsonType.String)]
    public Guid CustomizationId { get; set; }

    [BsonElement("productId"), BsonRepresentation(BsonType.String)]
    public Guid ProductId { get; set; }

    [BsonElement("productName")]
    public string ProductName { get; set; } = string.Empty;

    [BsonElement("productDescription"), BsonIgnoreIfNull]
    public string? ProductDescription { get; set; }

    [BsonElement("productColor")]
    public string ProductColor { get; set; } = string.Empty;

    [BsonElement("productSize")]
    public string ProductSize { get; set; } = string.Empty;

    [BsonElement("productImagePath"), BsonIgnoreIfNull]
    public string? ProductImagePath { get; set; }

    [BsonElement("productPrice"), BsonRepresentation(BsonType.Decimal128)]
    public decimal ProductPrice { get; set; }

    [BsonElement("designId"), BsonRepresentation(BsonType.String)]
    public Guid DesignId { get; set; }

    [BsonElement("designName")]
    public string DesignName { get; set; } = string.Empty;

    [BsonElement("designPrice"), BsonRepresentation(BsonType.Decimal128)]
    public decimal DesignPrice { get; set; }

    [BsonElement("designImagePath"), BsonIgnoreIfNull]
    public string? DesignImagePath { get; set; }

    [BsonElement("customizationDescription"), BsonIgnoreIfNull]
    public string? CustomizationDescription { get; set; }

    [BsonElement("unitPrice"), BsonRepresentation(BsonType.Decimal128)]
    public decimal UnitPrice { get; set; }

    [BsonElement("quantity")]
    public int Quantity { get; set; } = 1;

    [BsonElement("lineTotal"), BsonRepresentation(BsonType.Decimal128)]
    public decimal LineTotal { get; set; }
}

public sealed class OrderCustomerSnapshot
{
    [BsonElement("userId"), BsonRepresentation(BsonType.String)]
    public Guid UserId { get; set; }

    [BsonElement("firstName")]
    public string FirstName { get; set; } = string.Empty;

    [BsonElement("lastName")]
    public string LastName { get; set; } = string.Empty;

    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("phoneNumber"), BsonIgnoreIfNull]
    public string? PhoneNumber { get; set; }

    [BsonElement("address"), BsonIgnoreIfNull]
    public string? Address { get; set; }

    [BsonElement("country"), BsonIgnoreIfNull]
    public string? Country { get; set; }

    [BsonElement("zip"), BsonIgnoreIfNull]
    public string? Zip { get; set; }

    [BsonElement("isGuest")]
    public bool IsGuest { get; set; }
}

public sealed class OrderDeliverySnapshot
{
    [BsonElement("deliveryMethod")]
    public string DeliveryMethod { get; set; } = "HomeDelivery";

    [BsonElement("packetaPointId"), BsonIgnoreIfNull]
    public string? PacketaPointId { get; set; }

    [BsonElement("packetaPointName"), BsonIgnoreIfNull]
    public string? PacketaPointName { get; set; }

    [BsonElement("packetaPointAddress"), BsonIgnoreIfNull]
    public string? PacketaPointAddress { get; set; }

    [BsonElement("deliveryFee"), BsonRepresentation(BsonType.Decimal128)]
    public decimal DeliveryFee { get; set; }
}

public sealed class OrderPricingSnapshot
{
    [BsonElement("currency")]
    public string Currency { get; set; } = "EUR";

    [BsonElement("itemsSubtotal"), BsonRepresentation(BsonType.Decimal128)]
    public decimal ItemsSubtotal { get; set; }

    [BsonElement("paymentFee"), BsonRepresentation(BsonType.Decimal128)]
    public decimal PaymentFee { get; set; }

    [BsonElement("deliveryFee"), BsonRepresentation(BsonType.Decimal128)]
    public decimal DeliveryFee { get; set; }

    [BsonElement("totalPrice"), BsonRepresentation(BsonType.Decimal128)]
    public decimal TotalPrice { get; set; }

    [BsonElement("taxRate"), BsonRepresentation(BsonType.Decimal128)]
    public decimal TaxRate { get; set; } = 0.20m;

    [BsonElement("taxAmount"), BsonRepresentation(BsonType.Decimal128)]
    public decimal TaxAmount { get; set; }
}
