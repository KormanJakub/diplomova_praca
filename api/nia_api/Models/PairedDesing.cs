using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson;

namespace nia_api.Models;

public class PairedDesing
{
    [BsonId(IdGenerator = typeof(AscendingGuidGenerator))]
    [BsonElement("_id") ,BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }
    [BsonElement("name") ,BsonRepresentation(BsonType.String)]
    public string? Name { get; set; }
    [BsonElement("designId")]
    public List<Design> DesignIds { get; set; } = new List<Design>();
    [BsonElement("createdAt"), BsonRepresentation(BsonType.DateTime)]
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    [BsonElement("updatedAt"), BsonRepresentation(BsonType.DateTime)]
    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
}