namespace nia_api.Domain.Configuration;

public record MethodValidationResult(bool IsAllowed, string? Reason = null, decimal Fee = 0.00m)
{
    public static MethodValidationResult Allowed(decimal fee = 0.00m) => new(true, null, fee);
    public static MethodValidationResult Denied(string reason) => new(false, reason);
}

public interface IMerchantConfigurationService
{
    Task<PublicStoreProfileResponse> GetPublicProfileAsync(CancellationToken ct = default);
    Task<MerchantConfiguration> GetConfigurationAsync(CancellationToken ct = default);
    Task<MerchantConfiguration> UpdateConfigurationAsync(MerchantConfiguration configuration, CancellationToken ct = default);
    Task<bool> IsPersonalizationEnabledAsync(CancellationToken ct = default);
    Task<MethodValidationResult> ValidatePaymentMethodAllowedAsync(string? paymentMethod, CancellationToken ct = default);
    Task<MethodValidationResult> ValidateDeliveryMethodAllowedAsync(string? deliveryMethod, CancellationToken ct = default);
}
