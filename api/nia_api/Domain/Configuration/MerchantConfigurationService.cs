using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using nia_api.Data;
using Stripe;

namespace nia_api.Domain.Configuration;

public class MerchantConfigurationService : IMerchantConfigurationService
{
    private readonly IMongoCollection<MerchantConfiguration> _collection;
    private readonly IConfiguration? _configuration;

    public MerchantConfigurationService(NiaDbContext context, IConfiguration? configuration = null)
    {
        _collection = context.MerchantSettings;
        _configuration = configuration;
    }

    public MerchantConfigurationService(IMongoCollection<MerchantConfiguration> collection, IConfiguration? configuration = null)
    {
        _collection = collection;
        _configuration = configuration;
    }

    public async Task<MerchantConfiguration> GetConfigurationAsync(CancellationToken ct = default)
    {
        var config = await _collection.Find(c => c.Id == "store_settings").FirstOrDefaultAsync(ct);
        return config ?? new MerchantConfiguration { Id = "store_settings" };
    }

    public async Task<PublicStoreProfileResponse> GetPublicProfileAsync(CancellationToken ct = default)
    {
        var config = await GetConfigurationAsync(ct);
        return PublicStoreProfileResponse.From(config);
    }

    public async Task<MerchantConfiguration> UpdateConfigurationAsync(MerchantConfiguration configuration, CancellationToken ct = default)
    {
        configuration.Id = "store_settings";
        configuration.UpdatedAt = DateTime.UtcNow;

        await _collection.ReplaceOneAsync(
            c => c.Id == "store_settings",
            configuration,
            new ReplaceOptions { IsUpsert = true },
            ct);

        return configuration;
    }

    public async Task<bool> IsPersonalizationEnabledAsync(CancellationToken ct = default)
    {
        var config = await GetConfigurationAsync(ct);
        return config.EnablePersonalization;
    }

    public async Task<MethodValidationResult> ValidatePaymentMethodAllowedAsync(string? paymentMethod, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(paymentMethod))
        {
            return MethodValidationResult.Denied("Payment method is required.");
        }

        var config = await GetConfigurationAsync(ct);
        var normalized = paymentMethod.Trim();

        if (string.Equals(normalized, "stripe", StringComparison.OrdinalIgnoreCase))
        {
            if (!config.EnableStripe)
            {
                return MethodValidationResult.Denied("Stripe payment method is disabled.");
            }

            if (_configuration != null)
            {
                var secretKey = _configuration["Stripe:SecretKey"];
                if (string.IsNullOrWhiteSpace(secretKey) && string.IsNullOrWhiteSpace(StripeConfiguration.ApiKey))
                {
                    return MethodValidationResult.Denied("Stripe payment gateway is not properly configured on this store.");
                }
            }

            return MethodValidationResult.Allowed(0.00m);
        }

        if (string.Equals(normalized, "dobierka", StringComparison.OrdinalIgnoreCase))
        {
            if (!config.EnableCashOnDelivery)
            {
                return MethodValidationResult.Denied("Cash on delivery (Dobierka) is disabled.");
            }

            return MethodValidationResult.Allowed(config.CashOnDeliveryFee);
        }

        if (string.Equals(normalized, "iban", StringComparison.OrdinalIgnoreCase))
        {
            if (!config.EnableBankTransfer)
            {
                return MethodValidationResult.Denied("Bank transfer (IBAN) is disabled.");
            }

            if (string.IsNullOrWhiteSpace(config.BankAccountIban))
            {
                return MethodValidationResult.Denied("Bank transfer IBAN is not configured on this store.");
            }

            return MethodValidationResult.Allowed(0.00m);
        }

        return MethodValidationResult.Denied($"Unknown payment method: '{paymentMethod}'.");
    }

    public async Task<MethodValidationResult> ValidateDeliveryMethodAllowedAsync(string? deliveryMethod, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(deliveryMethod))
        {
            return MethodValidationResult.Denied("Delivery method is required.");
        }

        var config = await GetConfigurationAsync(ct);
        var normalized = deliveryMethod.Trim();

        if (string.Equals(normalized, "homedelivery", StringComparison.OrdinalIgnoreCase))
        {
            if (!config.EnableHomeDelivery)
            {
                return MethodValidationResult.Denied("Home delivery is disabled.");
            }

            return MethodValidationResult.Allowed(config.HomeDeliveryFee);
        }

        if (string.Equals(normalized, "packeta", StringComparison.OrdinalIgnoreCase))
        {
            if (!config.EnablePacketa)
            {
                return MethodValidationResult.Denied("Packeta delivery is disabled.");
            }

            if (string.IsNullOrWhiteSpace(config.PacketaApiKey))
            {
                return MethodValidationResult.Denied("Packeta API key is not configured on this store.");
            }

            return MethodValidationResult.Allowed(config.PacketaFee);
        }

        return MethodValidationResult.Denied($"Unknown delivery method: '{deliveryMethod}'.");
    }
}
