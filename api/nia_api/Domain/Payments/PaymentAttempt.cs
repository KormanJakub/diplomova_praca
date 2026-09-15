using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace nia_api.Domain.Payments;

public enum PaymentAttemptStatus
{
    Initiated,
    Pending,
    Paid,
    Failed,
    Cancelled,
    RefundNeeded,
    Refunded
}

public class PaymentAttempt
{
    [BsonId]
    [BsonElement("_id"), BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [BsonElement("orderId")]
    public int OrderId { get; set; }

    [BsonElement("attemptNumber")]
    public int AttemptNumber { get; set; } = 1;

    [BsonElement("provider")]
    public string Provider { get; set; } = "Stripe";

    [BsonElement("paymentMethod")]
    public string PaymentMethod { get; set; } = "Stripe";

    [BsonElement("amount"), BsonRepresentation(BsonType.Decimal128)]
    public decimal Amount { get; set; }

    [BsonElement("currency")]
    public string Currency { get; set; } = "EUR";

    [BsonElement("status"), BsonRepresentation(BsonType.String)]
    public PaymentAttemptStatus Status { get; set; } = PaymentAttemptStatus.Initiated;

    [BsonElement("externalReferenceId"), BsonIgnoreIfNull]
    public string? ExternalReferenceId { get; set; }

    [BsonElement("paymentIntentId"), BsonIgnoreIfNull]
    public string? PaymentIntentId { get; set; }

    [BsonElement("sessionUrl"), BsonIgnoreIfNull]
    public string? SessionUrl { get; set; }

    [BsonElement("idempotencyKey"), BsonIgnoreIfNull]
    public string? IdempotencyKey { get; set; }

    [BsonElement("failureReason"), BsonIgnoreIfNull]
    public string? FailureReason { get; set; }

    [BsonElement("auditNotes")]
    public List<string> AuditNotes { get; set; } = new();

    [BsonElement("createdAt"), BsonRepresentation(BsonType.DateTime)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("completedAt"), BsonRepresentation(BsonType.DateTime), BsonIgnoreIfNull]
    public DateTime? CompletedAt { get; set; }

    [BsonElement("refundedAt"), BsonRepresentation(BsonType.DateTime), BsonIgnoreIfNull]
    public DateTime? RefundedAt { get; set; }

    [BsonElement("refundReference"), BsonIgnoreIfNull]
    public string? RefundReference { get; set; }
}
