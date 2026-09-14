using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using nia_api.Services;

namespace nia_api.Models;

public class StoreSettings
{
    [BsonId]
    [BsonElement("_id"), BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = "store_settings";

    [BsonElement("cashOnDeliveryFee"), BsonRepresentation(BsonType.Decimal128)]
    public decimal CashOnDeliveryFee { get; set; } = 1.00m;

    [BsonElement("packetaApiKey"), BsonRepresentation(BsonType.String)]
    public string? PacketaApiKey { get; set; }

    [BsonElement("updatedAt"), BsonRepresentation(BsonType.DateTime)]
    public DateTime? UpdatedAt { get; set; } = LocalTimeService.LocalTime();
}
