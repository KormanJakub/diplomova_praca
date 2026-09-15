namespace nia_api.Domain.Payments;

public record GatewaySessionRequest(
    int OrderId,
    decimal Amount,
    string Currency,
    string CancellationToken,
    string SuccessUrl,
    string CancelUrl,
    string? CustomerEmail = null,
    IReadOnlyDictionary<string, string>? Metadata = null);

public record GatewaySessionResult(
    bool IsSuccess,
    string? SessionId = null,
    string? Url = null,
    string? ErrorMessage = null);

public record GatewayVerificationResult(
    bool IsSuccess,
    string? SessionId = null,
    string? PaymentStatus = null,
    string? Currency = null,
    long AmountTotal = 0,
    string? ClientReferenceId = null,
    string? PaymentIntentId = null,
    string? ErrorMessage = null);

public record GatewayRefundRequest(
    string PaymentReference,
    decimal? Amount = null,
    string? Reason = null);

public record GatewayRefundResult(
    bool IsSuccess,
    string? RefundId = null,
    string? Status = null,
    string? ErrorMessage = null);

public interface IPaymentGateway
{
    Task<GatewaySessionResult> CreateSessionAsync(GatewaySessionRequest request, CancellationToken ct = default);
    Task<GatewayVerificationResult> VerifySessionAsync(string sessionId, CancellationToken ct = default);
    Task<GatewayRefundResult> RefundPaymentAsync(GatewayRefundRequest request, CancellationToken ct = default);
}
