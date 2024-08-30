using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson;
using nia_api.Services;

namespace nia_api.Models;

public class Product
{
    [BsonId(IdGenerator = typeof(AscendingGuidGenerator))]
    [BsonElement("_id"), BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }
    [BsonElement("tagId") ,BsonRepresentation(BsonType.String)]
    public Guid TagId { get; set; }
    [BsonElement("name"), BsonRepresentation(BsonType.String)]
    public string? Name { get; set; }
    [BsonElement("description"), BsonRepresentation(BsonType.String)]
    public string? Description { get; set; }
    [BsonElement("colors")]
    public List<Colors> Colors { get; set; } = new List<Colors>();
    [BsonElement("price"), BsonRepresentation(BsonType.Decimal128)]
    public decimal Price { get; set; }
    /*
    [BsonElement("imageUrl"), BsonRepresentation(BsonType.String)]
    public string ImageUrl { get; set; } 
    */
    [BsonElement("createdAt"), BsonRepresentation(BsonType.DateTime)]
    public DateTime? CreatedAt { get; set; }

    [BsonElement("updatedAt"), BsonRepresentation(BsonType.DateTime)]
    public DateTime? UpdatedAt { get; set; } = LocalTimeService.LocalTime();
}

public class Colors
{
    [BsonElement("name"), BsonRepresentation(BsonType.String)]
    public string? Name { get; set; }
    [BsonElement("sizes")]
    public List<SizeInfo> Sizes { get; set; } = new List<SizeInfo>();
}

public class SizeInfo
{
    [BsonElement("size"), BsonRepresentation(BsonType.String)]
    public string? Size { get; set; }
    [BsonElement("quantity"), BsonRepresentation(BsonType.Int64)]
    public int Quantity { get; set; }
}

