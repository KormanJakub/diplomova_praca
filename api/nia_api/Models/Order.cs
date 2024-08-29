using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson;

namespace nia_api.Models;

public class Order
{
    [BsonId(IdGenerator = typeof(AscendingGuidGenerator))]
    [BsonElement("_id") ,BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }
}