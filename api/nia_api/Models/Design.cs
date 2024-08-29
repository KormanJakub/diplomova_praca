using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson;

namespace nia_api.Models;

public class Designň
{
    [BsonId(IdGenerator = typeof(AscendingGuidGenerator))]
    [BsonElement("_id") ,BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }
    [BsonElement("name"), BsonRepresentation(BsonType.String)]
    public string? Name { get; set; }
    [BsonElement("description"), BsonRepresentation(BsonType.String)]
    public string? Description { get; set; }
    [BsonElement("price"), BsonRepresentation(BsonType.Double)]
    public double Price {  get; set; }
    /*
     * TODO: Pridať tagy
     * TODO: Pridať na danú vec údaje (napr. mikina cervená, toľko kusov)
     */
    [BsonElement("createdAt"), BsonRepresentation(BsonType.DateTime)]
    public DateTime? CreatedAt { get; set; }
    [BsonElement("updatedAt"), BsonRepresentation(BsonType.DateTime)]
    public DateTime? UpdatedAt { get; set; }
}