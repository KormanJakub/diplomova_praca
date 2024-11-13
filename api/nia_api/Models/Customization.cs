using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson;
using nia_api.Services;

namespace nia_api.Models;

public class Customization
{
    [BsonId(IdGenerator = typeof(AscendingGuidGenerator))]
    [BsonElement("_id") ,BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }
    [BsonElement("designId") ,BsonRepresentation(BsonType.String)]
    public string? DesignId { get; set; }
    [BsonElement("productId") ,BsonRepresentation(BsonType.String)]
    public string? ProductId { get; set; }
    [BsonElement("userDescription") ,BsonRepresentation(BsonType.String)]
    public string? UserDescription { get; set; }
    [BsonElement("userId") ,BsonRepresentation(BsonType.String)]
    public string? UserId { get; set; }
    [BsonElement("price"), BsonRepresentation(BsonType.Decimal128)]
    public decimal Price { get; set; }
    [BsonElement("createdAt"), BsonRepresentation(BsonType.DateTime)]
    public DateTime? CreatedAt { get; set; }
    [BsonElement("updatedAt"), BsonRepresentation(BsonType.DateTime)]
    public DateTime? UpdatedAt { get; set; } = LocalTimeService.LocalTime();
}