using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson;
using nia_api.Services;

namespace nia_api.Models;

public class PairedDesign
{
    [BsonId(IdGenerator = typeof(AscendingGuidGenerator))]
    [BsonElement("_id") ,BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }
    [BsonElement("name") ,BsonRepresentation(BsonType.String)]
    public string? Name { get; set; }
    [BsonElement("designId")]
    public List<Guid> DesignIds { get; set; } = new List<Guid>();
    [BsonElement("createdAt"), BsonRepresentation(BsonType.DateTime)]
    public DateTime? CreatedAt { get; set; }
    [BsonElement("updatedAt"), BsonRepresentation(BsonType.DateTime)]
    public DateTime? UpdatedAt { get; set; } = LocalTimeService.LocalTime();
}