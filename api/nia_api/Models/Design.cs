using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson;

namespace nia_api.Models;

public class Design
{
    [BsonId(IdGenerator = typeof(AscendingGuidGenerator))]
    [BsonElement("_id") ,BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }
    [BsonElement("name"), BsonRepresentation(BsonType.String)]
    public string? Name { get; set; }
    [BsonElement("imageUrl"), BsonRepresentation(BsonType.String)]
    public string ImageUrl { get; set; } 
    [BsonElement("price"), BsonRepresentation(BsonType.Decimal128)]
    public decimal Price { get; set; }
    [BsonElement("createdAt"), BsonRepresentation(BsonType.DateTime)]
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    [BsonElement("updatedAt"), BsonRepresentation(BsonType.DateTime)]
    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
}