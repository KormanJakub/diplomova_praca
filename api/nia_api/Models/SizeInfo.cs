using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson;

namespace nia_api.Models;

public class SizeInfo
{
    [BsonElement("size"), BsonRepresentation(BsonType.String)]
    public string? Size { get; set; }
    [BsonElement("quantity"), BsonRepresentation(BsonType.Int64)]
    public int Quantity { get; set; }
    [BsonElement("createdAt"), BsonRepresentation(BsonType.DateTime)]
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    [BsonElement("updatedAt"), BsonRepresentation(BsonType.DateTime)]
    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
}