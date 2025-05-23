﻿using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson;
using nia_api.Enums;
using nia_api.Services;

namespace nia_api.Models;

public class Order
{
    [BsonId(IdGenerator = typeof(NullIdChecker))]
    [BsonElement("_id"), BsonRepresentation(BsonType.Int64)]
    public int Id { get; set; }
    [BsonElement("customizations"), BsonRepresentation(BsonType.String)]
    public List<Guid> Customizations { get; set; } = new List<Guid>();

    [BsonElement("totalPrice"), BsonRepresentation(BsonType.Decimal128)]
    public decimal TotalPrice { get; set; }
    [BsonElement("userId"), BsonRepresentation(BsonType.String)]
    public Guid UserId { get; set; }
    
    [BsonElement("statusOrder"), BsonRepresentation(BsonType.String)]
    public EStatus StatusOrder { get; set; }
    [BsonElement("paymentId"), BsonRepresentation(BsonType.String)]
    public string PaymentId { get; set; }

    [BsonElement("paymentStatus"), BsonRepresentation(BsonType.String)]
    public string PaymentStatus { get; set; }
    [BsonElement("cancellationToken"), BsonRepresentation(BsonType.String)]
    public string CancellationToken { get; set; }
    [BsonElement("followToken"), BsonRepresentation(BsonType.String)]
    public string FollowToken { get; set; }
    [BsonElement("createdAt"), BsonRepresentation(BsonType.DateTime)]
    public DateTime? CreatedAt { get; set; }
    [BsonElement("updatedAt"), BsonRepresentation(BsonType.DateTime)]
    public DateTime? UpdatedAt { get; set; } = LocalTimeService.LocalTime();
}