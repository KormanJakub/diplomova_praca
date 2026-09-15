using nia_api.Data;
using nia_api.Models;
using nia_api.Services;

namespace nia_api.Tests;

public class OrderServiceTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public OrderServiceTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private OrderService CreateOrderService()
    {
        return new OrderService(_factory.GetDbContext());
    }

    [Fact]
    public async Task CreateAsync_ReturnsInvalidItems_WhenCustomizationListIsNull()
    {
        var service = CreateOrderService();
        var result = await service.CreateAsync(Guid.NewGuid(), null);

        Assert.Null(result.Order);
        Assert.Equal(OrderCreationError.InvalidItems, result.Error);
    }

    [Fact]
    public async Task CreateAsync_ReturnsInvalidItems_WhenCustomizationListIsEmpty()
    {
        var service = CreateOrderService();
        var result = await service.CreateAsync(Guid.NewGuid(), new List<Guid>());

        Assert.Null(result.Order);
        Assert.Equal(OrderCreationError.InvalidItems, result.Error);
    }

    [Fact]
    public async Task CreateAsync_ReturnsInvalidItems_WhenDuplicateCustomizationIdsProvided()
    {
        var service = CreateOrderService();
        var duplicateId = Guid.NewGuid();
        var list = new List<Guid> { duplicateId, duplicateId };

        var result = await service.CreateAsync(Guid.NewGuid(), list);

        Assert.Null(result.Order);
        Assert.Equal(OrderCreationError.InvalidItems, result.Error);
    }

    [Fact]
    public async Task CreateAsync_ReturnsInvalidItems_WhenCustomizationDoesNotExistInDatabase()
    {
        var service = CreateOrderService();
        var missingId = Guid.NewGuid();
        var list = new List<Guid> { missingId };

        var result = await service.CreateAsync(Guid.NewGuid(), list);

        Assert.Null(result.Order);
        Assert.Equal(OrderCreationError.InvalidItems, result.Error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CancelByTokenAsync_ReturnsFalse_WhenTokenIsBlank(string? token)
    {
        var service = CreateOrderService();
        var result = await service.CancelByTokenAsync(token!);

        Assert.False(result);
    }

    [Fact]
    public async Task CancelByUserAsync_ReturnsFalse_WhenOrderDoesNotExist()
    {
        var service = CreateOrderService();
        var result = await service.CancelByUserAsync(999999, Guid.NewGuid());

        Assert.False(result);
    }

    [Fact]
    public async Task CancelByAdminAsync_ReturnsFalse_WhenOrderDoesNotExist()
    {
        var service = CreateOrderService();
        var result = await service.CancelByAdminAsync(999999);

        Assert.False(result);
    }

    [Fact]
    public void StoreSettings_DefaultValues_AreCorrect()
    {
        var settings = new StoreSettings();
        Assert.Equal("store_settings", settings.Id);
        Assert.Equal(1.00m, settings.CashOnDeliveryFee);
        Assert.Null(settings.PacketaApiKey);
        Assert.NotNull(settings.UpdatedAt);
    }

    [Fact]
    public void Order_DefaultValues_IncludePaymentMethodAndFee()
    {
        var order = new Order();
        Assert.Equal("Stripe", order.PaymentMethod);
        Assert.Equal(0.00m, order.PaymentFee);
        Assert.Equal("HomeDelivery", order.DeliveryMethod);
        Assert.Null(order.PacketaPointId);
        Assert.Null(order.PacketaPointName);
        Assert.Null(order.PacketaPointAddress);
    }
}
