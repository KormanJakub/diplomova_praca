﻿using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson;
using nia_api.Services;

namespace nia_api.Models;

public class Questions
{
    [BsonId(IdGenerator = typeof(AscendingGuidGenerator))]
    [BsonElement("_id") ,BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }
    [BsonElement("name"), BsonRepresentation(BsonType.String)]
    public string? Name { get; set; }
    [BsonElement("email"), BsonRepresentation(BsonType.String)]
    public string? Email { get; set; }
    [BsonElement("description"), BsonRepresentation(BsonType.String)]
    public string? Description { get; set; }
    [BsonElement("fileId"), BsonRepresentation(BsonType.String)]
    public string? FileId { get; set; }
    [BsonElement("pathOfFile"), BsonRepresentation(BsonType.String)]
    public string? PathOfFile { get; set; }
    [BsonElement("createdAt"), BsonRepresentation(BsonType.DateTime)]
    public DateTime? CreatedAt { get; set; }
    [BsonElement("updatedAt"), BsonRepresentation(BsonType.DateTime)]
    public DateTime? UpdatedAt { get; set; } = LocalTimeService.LocalTime();
}