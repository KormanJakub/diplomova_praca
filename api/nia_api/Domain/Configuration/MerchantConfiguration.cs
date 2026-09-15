using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace nia_api.Domain.Configuration;

[BsonIgnoreExtraElements]
public class MerchantConfiguration
{
    [BsonId]
    [BsonElement("_id"), BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = "store_settings";

    // Store Profile
    [BsonElement("storeName")]
    public string StoreName { get; set; } = "KorSoft ESHOP";

    [BsonElement("logoUrl"), BsonIgnoreIfNull]
    public string? LogoUrl { get; set; }

    [BsonElement("contactEmail"), BsonIgnoreIfNull]
    public string? ContactEmail { get; set; } = "info@korsoft.sk";

    [BsonElement("contactPhone"), BsonIgnoreIfNull]
    public string? ContactPhone { get; set; }

    [BsonElement("currency")]
    public string Currency { get; set; } = "EUR";

    [BsonElement("defaultLocale")]
    public string DefaultLocale { get; set; } = "sk";

    // Module Entitlements
    [BsonElement("enablePersonalization")]
    public bool EnablePersonalization { get; set; } = true;

    [BsonElement("enableReviews")]
    public bool EnableReviews { get; set; } = false;

    [BsonElement("enableCoupons")]
    public bool EnableCoupons { get; set; } = false;

    [BsonElement("enableAdvancedReporting")]
    public bool EnableAdvancedReporting { get; set; } = false;

    // Payment Configuration
    [BsonElement("enableStripe")]
    public bool EnableStripe { get; set; } = true;

    [BsonElement("enableCashOnDelivery")]
    public bool EnableCashOnDelivery { get; set; } = true;

    [BsonElement("cashOnDeliveryFee"), BsonRepresentation(BsonType.Decimal128)]
    public decimal CashOnDeliveryFee { get; set; } = 1.00m;

    [BsonElement("enableBankTransfer")]
    public bool EnableBankTransfer { get; set; } = true;

    [BsonElement("bankAccountIban"), BsonIgnoreIfNull]
    public string? BankAccountIban { get; set; }

    [BsonElement("bankAccountBic"), BsonIgnoreIfNull]
    public string? BankAccountBic { get; set; }

    [BsonElement("bankTransferInstructions"), BsonIgnoreIfNull]
    public string? BankTransferInstructions { get; set; } = "Uveďte číslo objednávky ako variabilný symbol.";

    // Delivery Configuration
    [BsonElement("enableHomeDelivery")]
    public bool EnableHomeDelivery { get; set; } = true;

    [BsonElement("homeDeliveryFee"), BsonRepresentation(BsonType.Decimal128)]
    public decimal HomeDeliveryFee { get; set; } = 0.00m;

    [BsonElement("enablePacketa")]
    public bool EnablePacketa { get; set; } = true;

    [BsonElement("packetaApiKey"), BsonIgnoreIfNull]
    public string? PacketaApiKey { get; set; }

    [BsonElement("packetaFee"), BsonRepresentation(BsonType.Decimal128)]
    public decimal PacketaFee { get; set; } = 0.00m;

    // Merchant Operations
    [BsonElement("timezone")]
    public string Timezone { get; set; } = "Europe/Bratislava";

    [BsonElement("vatPayer")]
    public bool VatPayer { get; set; } = true;

    [BsonElement("vatRate"), BsonRepresentation(BsonType.Decimal128)]
    public decimal VatRate { get; set; } = 0.20m;

    [BsonElement("companyRegistrationNumber"), BsonIgnoreIfNull]
    public string? CompanyRegistrationNumber { get; set; }

    [BsonElement("taxRegistrationNumber"), BsonIgnoreIfNull]
    public string? TaxRegistrationNumber { get; set; }

    [BsonElement("vatRegistrationNumber"), BsonIgnoreIfNull]
    public string? VatRegistrationNumber { get; set; }

    [BsonElement("billingAddress"), BsonIgnoreIfNull]
    public string? BillingAddress { get; set; }

    [BsonElement("updatedAt"), BsonRepresentation(BsonType.DateTime)]
    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class PublicStoreProfileResponse
{
    public string Id { get; set; } = "store_settings";
    public string StoreName { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string Currency { get; set; } = "EUR";
    public string DefaultLocale { get; set; } = "sk";

    // Legacy and fee properties for Angular checkout
    public decimal CashOnDeliveryFee { get; set; }
    public string? PacketaApiKey { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Module entitlements and payment/delivery flags
    public bool EnablePersonalization { get; set; }
    public bool EnableStripe { get; set; }
    public bool EnableCashOnDelivery { get; set; }
    public bool EnableBankTransfer { get; set; }
    public string? BankAccountIban { get; set; }
    public string? BankAccountBic { get; set; }
    public string? BankTransferInstructions { get; set; }
    public bool EnableHomeDelivery { get; set; }
    public decimal HomeDeliveryFee { get; set; }
    public bool EnablePacketa { get; set; }
    public decimal PacketaFee { get; set; }

    public static PublicStoreProfileResponse From(MerchantConfiguration config)
    {
        return new PublicStoreProfileResponse
        {
            Id = config.Id,
            StoreName = config.StoreName,
            LogoUrl = config.LogoUrl,
            ContactEmail = config.ContactEmail,
            ContactPhone = config.ContactPhone,
            Currency = config.Currency,
            DefaultLocale = config.DefaultLocale,
            CashOnDeliveryFee = config.CashOnDeliveryFee,
            PacketaApiKey = config.EnablePacketa ? config.PacketaApiKey : null,
            UpdatedAt = config.UpdatedAt,
            EnablePersonalization = config.EnablePersonalization,
            EnableStripe = config.EnableStripe,
            EnableCashOnDelivery = config.EnableCashOnDelivery,
            EnableBankTransfer = config.EnableBankTransfer,
            BankAccountIban = config.EnableBankTransfer ? config.BankAccountIban : null,
            BankAccountBic = config.EnableBankTransfer ? config.BankAccountBic : null,
            BankTransferInstructions = config.EnableBankTransfer ? config.BankTransferInstructions : null,
            EnableHomeDelivery = config.EnableHomeDelivery,
            HomeDeliveryFee = config.HomeDeliveryFee,
            EnablePacketa = config.EnablePacketa,
            PacketaFee = config.PacketaFee
        };
    }
}
