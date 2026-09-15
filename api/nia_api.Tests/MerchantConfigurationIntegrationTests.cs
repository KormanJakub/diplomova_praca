using System.Net;
using System.Net.Http.Json;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using nia_api.Controllers;
using nia_api.Data;
using nia_api.Domain.Configuration;
using nia_api.Domain.Orders;
using nia_api.Enums;
using nia_api.Models;
using nia_api.Requests;
using nia_api.Services;

namespace nia_api.Tests;

public class MerchantConfigurationIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public MerchantConfigurationIntegrationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task StoreProfile_PublicEndpoint_ReturnsSafeBrandingWithoutSecrets()
    {
        // Arrange
        var db = _factory.GetDbContext();
        var client = _factory.CreateClient();

        var config = new MerchantConfiguration
        {
            Id = "store_settings",
            StoreName = "Nordic Hardware",
            ContactEmail = "support@nordichardware.no",
            Currency = "EUR",
            EnablePersonalization = false,
            EnableStripe = false,
            EnableBankTransfer = true,
            BankAccountIban = "SK99000000001234567890",
            BankAccountBic = "NORDICSK",
            BankTransferInstructions = "Use order number as variable symbol.",
            EnableHomeDelivery = true,
            HomeDeliveryFee = 3.50m,
            EnablePacketa = true,
            PacketaApiKey = "packeta_safe_public_key_123",
            PacketaFee = 2.00m
        };

        await db.MerchantSettings.ReplaceOneAsync(
            c => c.Id == "store_settings",
            config,
            new ReplaceOptions { IsUpsert = true });

        // Act
        var response = await client.GetAsync("/public/store-settings");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        var profile = JObject.Parse(json);

        Assert.Equal("Nordic Hardware", (string?)profile["StoreName"]);
        Assert.Equal("support@nordichardware.no", (string?)profile["ContactEmail"]);
        Assert.Equal("EUR", (string?)profile["Currency"]);
        Assert.False((bool?)profile["EnablePersonalization"]);
        Assert.False((bool?)profile["EnableStripe"]);
        Assert.True((bool?)profile["EnableBankTransfer"]);
        Assert.Equal("SK99000000001234567890", (string?)profile["BankAccountIban"]);
        Assert.Equal("packeta_safe_public_key_123", (string?)profile["PacketaApiKey"]);
        Assert.Equal(3.50m, (decimal?)profile["HomeDeliveryFee"]);
    }

    [Fact]
    public async Task Unconfigured_BankTransfer_FailsClosed()
    {
        // Arrange
        var db = _factory.GetDbContext();
        var sequenceStore = new MongoOrderSequenceStore(db);
        var configService = new MerchantConfigurationService(db);
        var orderService = new OrderService(db, sequenceStore, configService);

        // Bank transfer is enabled, but IBAN is null/empty
        var config = new MerchantConfiguration
        {
            Id = "store_settings",
            EnableBankTransfer = true,
            BankAccountIban = null // Not configured!
        };
        await db.MerchantSettings.ReplaceOneAsync(c => c.Id == "store_settings", config, new ReplaceOptions { IsUpsert = true });

        var user = await _factory.SeedUserAsync();
        var (productId, customizationId) = await SeedProductAndCustomizationAsync(db, user.Id, hasDesign: false);

        // Act: Attempt to create order using unconfigured IBAN
        var result = await orderService.CreateAsync(
            user.Id,
            new List<Guid> { customizationId },
            paymentMethod: "IBAN",
            deliveryMethod: "HomeDelivery");

        // Assert: Must fail closed
        Assert.Null(result.Order);
        Assert.Equal(OrderCreationError.InvalidItems, result.Error);
    }

    [Fact]
    public async Task Configured_BankTransfer_Succeeds()
    {
        // Arrange
        var db = _factory.GetDbContext();
        var sequenceStore = new MongoOrderSequenceStore(db);
        var configService = new MerchantConfigurationService(db);
        var orderService = new OrderService(db, sequenceStore, configService);

        var config = new MerchantConfiguration
        {
            Id = "store_settings",
            EnableBankTransfer = true,
            BankAccountIban = "SK12345678901234567890",
            EnablePersonalization = false
        };
        await db.MerchantSettings.ReplaceOneAsync(c => c.Id == "store_settings", config, new ReplaceOptions { IsUpsert = true });

        var user = await _factory.SeedUserAsync();
        var (productId, customizationId) = await SeedProductAndCustomizationAsync(db, user.Id, hasDesign: false);

        // Act
        var result = await orderService.CreateAsync(
            user.Id,
            new List<Guid> { customizationId },
            paymentMethod: "IBAN",
            deliveryMethod: "HomeDelivery");

        // Assert
        Assert.NotNull(result.Order);
        Assert.Null(result.Error);
        Assert.Equal("IBAN", result.Order.PaymentMethod);
        Assert.Equal("HomeDelivery", result.Order.DeliveryMethod);
    }

    [Fact]
    public async Task Unconfigured_Packeta_FailsClosed()
    {
        // Arrange
        var db = _factory.GetDbContext();
        var sequenceStore = new MongoOrderSequenceStore(db);
        var configService = new MerchantConfigurationService(db);
        var orderService = new OrderService(db, sequenceStore, configService);

        // Packeta is enabled, but PacketaApiKey is null
        var config = new MerchantConfiguration
        {
            Id = "store_settings",
            EnablePacketa = true,
            PacketaApiKey = null // Not configured!
        };
        await db.MerchantSettings.ReplaceOneAsync(c => c.Id == "store_settings", config, new ReplaceOptions { IsUpsert = true });

        var user = await _factory.SeedUserAsync();
        var (productId, customizationId) = await SeedProductAndCustomizationAsync(db, user.Id, hasDesign: true);

        // Act: Attempt to place order with unconfigured Packeta
        var result = await orderService.CreateAsync(
            user.Id,
            new List<Guid> { customizationId },
            paymentMethod: "Dobierka",
            deliveryMethod: "Packeta",
            packetaPointId: "12345",
            packetaPointName: "Point A",
            packetaPointAddress: "Address A");

        // Assert: Must fail closed
        Assert.Null(result.Order);
        Assert.Equal(OrderCreationError.InvalidItems, result.Error);
    }

    [Fact]
    public async Task Disabled_Personalization_Blocks_Design_Queries()
    {
        // Arrange
        var db = _factory.GetDbContext();
        var config = new MerchantConfiguration
        {
            Id = "store_settings",
            EnablePersonalization = false
        };
        await db.MerchantSettings.ReplaceOneAsync(c => c.Id == "store_settings", config, new ReplaceOptions { IsUpsert = true });

        var publicClient = _factory.CreateClient();
        var adminUser = await _factory.SeedUserAsync(isAdmin: true);
        var adminClient = _factory.CreateAuthenticatedClient(adminUser, origin: ApiWebApplicationFactory.AllowedWebOrigin);

        // Act 1: Public designs endpoint
        var publicResponse = await publicClient.GetAsync("/public/all-designs");
        Assert.Equal(HttpStatusCode.Forbidden, publicResponse.StatusCode);

        // Act 2: Admin get designs endpoint
        var adminGetResponse = await adminClient.GetAsync("/admin/design/getAll");
        Assert.Equal(HttpStatusCode.Forbidden, adminGetResponse.StatusCode);

        // Act 3: Admin create design endpoint
        var adminCreateResponse = await adminClient.PostAsJsonAsync("/admin/design/create", new Design
        {
            Name = "Forbidden Design",
            Price = 10.00m,
            FileId = "f1"
        });
        Assert.Equal(HttpStatusCode.Forbidden, adminCreateResponse.StatusCode);
    }

    [Fact]
    public async Task Disabled_Personalization_Blocks_Customization_Creation()
    {
        // Arrange
        var db = _factory.GetDbContext();
        var config = new MerchantConfiguration
        {
            Id = "store_settings",
            EnablePersonalization = false
        };
        await db.MerchantSettings.ReplaceOneAsync(c => c.Id == "store_settings", config, new ReplaceOptions { IsUpsert = true });

        var designId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var client = _factory.CreateClient();

        var request = new GuestCustomizationRequest
        {
            GuestData = new GuestData
            {
                FirstName = "Anna",
                LastName = "Kovácsová",
                Email = "anna@example.com",
                Address = "Hlavná 1",
                Country = "Slovakia",
                Zip = "04001",
                PhoneNumber = "+421900111222"
            },
            Customizations = new List<CustomizationRequest>
            {
                new()
                {
                    DesignId = designId.ToString(), // Trying to use design when personalization disabled!
                    ProductId = productId.ToString(),
                    ProductColorName = "White",
                    ProductSize = "M"
                }
            }
        };

        // Act
        var response = await client.PostAsJsonAsync("/guest/make-customization-without-register", request);

        // Assert: Must be rejected with 403 Forbidden
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Plain_Catalog_Merchant_Checkout_Succeeds_Without_Personalization()
    {
        // Arrange
        var db = _factory.GetDbContext();
        var config = new MerchantConfiguration
        {
            Id = "store_settings",
            StoreName = "Nordic Hardware",
            EnablePersonalization = false,
            EnableBankTransfer = true,
            BankAccountIban = "SK99000000008888888888",
            EnableHomeDelivery = true,
            HomeDeliveryFee = 4.00m
        };
        await db.MerchantSettings.ReplaceOneAsync(c => c.Id == "store_settings", config, new ReplaceOptions { IsUpsert = true });

        // Seed plain hardware product with stock
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Mechanical Keyboard",
            Price = 120.00m,
            Colors = new List<Colors>
            {
                new()
                {
                    Name = "Charcoal",
                    FileId = "f1",
                    Sizes = new List<SizeInfo>
                    {
                        new() { Size = "Standard", Quantity = 10 }
                    }
                }
            }
        };
        await db.Products.InsertOneAsync(product);

        var client = _factory.CreateClient();

        // 1. Guest buys plain product without design
        var guestRequest = new GuestCustomizationRequest
        {
            GuestData = new GuestData
            {
                FirstName = "Erik",
                LastName = "Larsen",
                Email = "erik@nordic.no",
                Address = "Storgata 10",
                Country = "Norway",
                Zip = "0155",
                PhoneNumber = "+4712345678"
            },
            Customizations = new List<CustomizationRequest>
            {
                new()
                {
                    DesignId = null, // Plain product, no design!
                    ProductId = product.Id.ToString(),
                    ProductColorName = "Charcoal",
                    ProductSize = "Standard"
                }
            }
        };

        var customResponse = await client.PostAsJsonAsync("/guest/make-customization-without-register", guestRequest);
        Assert.Equal(HttpStatusCode.OK, customResponse.StatusCode);

        var customJson = await customResponse.Content.ReadAsStringAsync();
        var customObj = JObject.Parse(customJson);
        var guestUserId = Guid.Parse((string)customObj["GuestUserId"]!);
        var customizationId = Guid.Parse((string)customObj["SuccessCustomization"]![0]!["Id"]!);

        // 2. Guest places order with IBAN & HomeDelivery
        var sequenceStore = new MongoOrderSequenceStore(db);
        var configService = new MerchantConfigurationService(db);
        var orderService = new OrderService(db, sequenceStore, configService);

        var orderResult = await orderService.CreateAsync(
            guestUserId,
            new List<Guid> { customizationId },
            paymentMethod: "IBAN",
            deliveryMethod: "HomeDelivery");

        Assert.NotNull(orderResult.Order);
        Assert.Null(orderResult.Error);
        Assert.Equal("IBAN", orderResult.Order.PaymentMethod);
        Assert.Equal("HomeDelivery", orderResult.Order.DeliveryMethod);
        Assert.Equal(4.00m, orderResult.Order.DeliveryFee);
        Assert.Equal(124.00m, orderResult.Order.TotalPrice); // 120 + 4 delivery
        Assert.Equal(Guid.Empty, orderResult.Order.Lines![0].DesignId);
        Assert.Equal("Neznámy dizajn", orderResult.Order.Lines![0].DesignName);
    }

    [Fact]
    public async Task Admin_Settings_Update_Persists_And_Enforces_New_Policies()
    {
        // Arrange
        var db = _factory.GetDbContext();
        var adminUser = await _factory.SeedUserAsync(isAdmin: true);
        var adminClient = _factory.CreateAuthenticatedClient(adminUser, origin: ApiWebApplicationFactory.AllowedWebOrigin);

        // Act: Update settings to disable Stripe and require IBAN
        var updateRequest = new UpdateSettingsRequest
        {
            StoreName = "Nordic Tools & Hardware",
            EnablePersonalization = false,
            EnableStripe = false,
            EnableBankTransfer = true,
            BankAccountIban = "SK55112233445566778899",
            EnableCashOnDelivery = true,
            CashOnDeliveryFee = 2.50m
        };

        var updateResponse = await adminClient.PutAsJsonAsync("/admin/settings", updateRequest);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        // Verify public store settings reflected the change
        var publicClient = _factory.CreateClient();
        var publicResponse = await publicClient.GetAsync("/public/store-settings");
        var publicJson = await publicResponse.Content.ReadAsStringAsync();
        var publicProfile = JObject.Parse(publicJson);

        Assert.Equal("Nordic Tools & Hardware", (string?)publicProfile["StoreName"]);
        Assert.False((bool?)publicProfile["EnableStripe"]);
        Assert.False((bool?)publicProfile["EnablePersonalization"]);
        Assert.True((bool?)publicProfile["EnableBankTransfer"]);
        Assert.Equal("SK55112233445566778899", (string?)publicProfile["BankAccountIban"]);
        Assert.Equal(2.50m, (decimal?)publicProfile["CashOnDeliveryFee"]);

        // Verify policy enforcement: Stripe is now rejected
        var sequenceStore = new MongoOrderSequenceStore(db);
        var configService = new MerchantConfigurationService(db);
        var orderService = new OrderService(db, sequenceStore, configService);

        var (productId, customizationId) = await SeedProductAndCustomizationAsync(db, adminUser.Id, hasDesign: false);

        var orderResult = await orderService.CreateAsync(
            adminUser.Id,
            new List<Guid> { customizationId },
            paymentMethod: "Stripe",
            deliveryMethod: "HomeDelivery");

        // Fails closed because Stripe was disabled
        Assert.Null(orderResult.Order);
        Assert.Equal(OrderCreationError.InvalidItems, orderResult.Error);
    }

    private static async Task<(Guid ProductId, Guid CustomizationId)> SeedProductAndCustomizationAsync(
        NiaDbContext db,
        Guid userId,
        bool hasDesign)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Test Shirt",
            Price = 25.00m,
            Colors = new List<Colors>
            {
                new()
                {
                    Name = "Black",
                    FileId = "f1",
                    Sizes = new List<SizeInfo>
                    {
                        new() { Size = "L", Quantity = 10 }
                    }
                }
            }
        };
        await db.Products.InsertOneAsync(product);

        Guid? designId = null;
        if (hasDesign)
        {
            var design = new Design
            {
                Id = Guid.NewGuid(),
                Name = "Test Print",
                Price = 5.00m
            };
            await db.Designs.InsertOneAsync(design);
            designId = design.Id;
        }

        var customization = new Customization
        {
            Id = Guid.NewGuid(),
            UserId = userId.ToString(),
            ProductId = product.Id.ToString(),
            DesignId = designId?.ToString(),
            ProductColor = "Black",
            ProductSize = "L",
            Price = product.Price + (hasDesign ? 5.00m : 0.00m),
            IsOrdered = false
        };
        await db.Customizations.InsertOneAsync(customization);

        return (product.Id, customization.Id);
    }
}
