using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace nia_api.Domain.Payments;

public enum WebhookStatus
{
    Received,
    Processing,
    Processed,
    Failed,
    Ignored
}

public class WebhookInboxMessage
{
    [BsonId]
    [BsonElement("_id"), BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [BsonElement("provider")]
    public string Provider { get; set; } = "Stripe";

    [BsonElement("eventId")]
    public string EventId { get; set; } = string.Empty;

    [BsonElement("eventType")]
    public string EventType { get; set; } = string.Empty;

    [BsonElement("payload")]
    public string Payload { get; set; } = string.Empty;

    [BsonElement("status"), BsonRepresentation(BsonType.String)]
    public WebhookStatus Status { get; set; } = WebhookStatus.Received;

    [BsonElement("receivedAt"), BsonRepresentation(BsonType.DateTime)]
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("processedAt"), BsonRepresentation(BsonType.DateTime), BsonIgnoreIfNull]
    public DateTime? ProcessedAt { get; set; }

    [BsonElement("errorMessage"), BsonIgnoreIfNull]
    public string? ErrorMessage { get; set; }
}
