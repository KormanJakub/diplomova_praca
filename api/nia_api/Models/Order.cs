using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson;

namespace nia_api.Models;

public class Order
{
    [BsonId(IdGenerator = typeof(AscendingGuidGenerator))]
    [BsonElement("_id"), BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }
    [BsonElement("customizations")]
    public List<Customization> Customizations { get; set; } = new List<Customization>();
    [BsonElement("totalPrice"), BsonRepresentation(BsonType.Decimal128)]
    public decimal TotalPrice { get; set; }
    [BsonElement("userId"), BsonRepresentation(BsonType.String)]
    public Guid UserId { get; set; }
    [BsonElement("createdAt"), BsonRepresentation(BsonType.DateTime)]
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    [BsonElement("updatedAt"), BsonRepresentation(BsonType.DateTime)]
    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
}