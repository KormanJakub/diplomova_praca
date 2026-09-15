using System.Net;
using System.Net.Http.Json;
using MongoDB.Driver;
using nia_api.Controllers;
using nia_api.Enums;
using nia_api.Models;
using nia_api.Requests;
using nia_api.Security;

namespace nia_api.Tests;

public class OrderMutationIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public OrderMutationIntegrationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AdminOrderUpdate_Anonymous_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateAnonymousClient();
        var updateRequest = new AdminUpdateOrderRequest
        {
            StatusOrder = EStatus.VO_VYROBE,
            DeliveryMethod = "HomeDelivery"
        };

        // Act
        var response = await client.PutAsJsonAsync("/admin/orders/999", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AdminOrderUpdate_CustomerRole_ReturnsForbidden()
    {
        // Arrange
        var customer = await _factory.SeedUserAsync(isAdmin: false);
        var client = _factory.CreateAuthenticatedClient(customer);
        var updateRequest = new AdminUpdateOrderRequest
        {
            StatusOrder = EStatus.VO_VYROBE,
            DeliveryMethod = "HomeDelivery"
        };

        // Act
        var response = await client.PutAsJsonAsync("/admin/orders/999", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminOrderUpdate_NonExistentOrder_ReturnsNotFound()
    {
        // Arrange
        var admin = await _factory.SeedUserAsync(isAdmin: true);
        var client = _factory.CreateAuthenticatedClient(admin);
        var updateRequest = new AdminUpdateOrderRequest
        {
            StatusOrder = EStatus.VO_VYROBE,
            DeliveryMethod = "HomeDelivery"
        };

        // Act
        var response = await client.PutAsJsonAsync("/admin/orders/999999", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AdminOrderCancel_NonExistentOrder_ReturnsNotFound()
    {
        // Arrange
        var admin = await _factory.SeedUserAsync(isAdmin: true);
        var client = _factory.CreateAuthenticatedClient(admin);

        // Act
        var response = await client.PostAsync("/admin/orders/cancel/999999", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GuestOrderCancel_ExpiredToken_ReturnsNotFound()
    {
        // Arrange: Create an order with an expired cancellation token
        var rawToken = CapabilityToken.Create();
        var hashedToken = CapabilityToken.Hash(rawToken);
        var orderId = new Random().Next(100000, 999999);

        var db = _factory.GetDbContext();
        var order = new Order
        {
            Id = orderId,
            UserId = Guid.NewGuid(),
            StatusOrder = EStatus.PRIJATA,
            CancellationToken = hashedToken,
            CancellationTokenExpiresAt = DateTime.UtcNow.AddHours(-2), // Expired 2 hours ago
            FollowToken = CapabilityToken.Hash(CapabilityToken.Create()),
            FollowTokenExpiresAt = DateTime.UtcNow.AddDays(180),
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            Customizations = new List<Guid>()
        };
        await db.Orders.InsertOneAsync(order);

        var client = _factory.CreateAnonymousClient();

        // Act
        var response = await client.PostAsJsonAsync("/guest/cancel-order-by-token", new CapabilityTokenRequest
        {
            Token = rawToken
        });

        // Assert: Rejects expired token
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GuestOrderCancel_InvalidOrForgedToken_ReturnsNotFound()
    {
        // Arrange
        var forgedToken = CapabilityToken.Create();
        var client = _factory.CreateAnonymousClient();

        // Act
        var response = await client.PostAsJsonAsync("/guest/cancel-order-by-token", new CapabilityTokenRequest
        {
            Token = forgedToken
        });

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GuestOrderCancel_ValidToken_SuccessfullyCancelsOrder_AndCannotBeCancelledAgain()
    {
        // Arrange: Seed valid order with unexpired token
        var rawToken = CapabilityToken.Create();
        var hashedToken = CapabilityToken.Hash(rawToken);
        var orderId = new Random().Next(100000, 999999);

        var db = _factory.GetDbContext();
        var order = new Order
        {
            Id = orderId,
            UserId = Guid.NewGuid(),
            StatusOrder = EStatus.PRIJATA,
            CancellationToken = hashedToken,
            CancellationTokenExpiresAt = DateTime.UtcNow.AddHours(20), // Valid
            FollowToken = CapabilityToken.Hash(CapabilityToken.Create()),
            FollowTokenExpiresAt = DateTime.UtcNow.AddDays(180),
            CreatedAt = DateTime.UtcNow,
            Customizations = new List<Guid>()
        };
        await db.Orders.InsertOneAsync(order);

        var client = _factory.CreateAnonymousClient();

        // Act 1: Cancel with valid token
        var response1 = await client.PostAsJsonAsync("/guest/cancel-order-by-token", new CapabilityTokenRequest
        {
            Token = rawToken
        });

        // Assert 1: Successful cancellation
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);

        // Verify status in DB is ZRUSENA
        var updatedOrder = await db.Orders.Find(o => o.Id == orderId).FirstOrDefaultAsync();
        Assert.NotNull(updatedOrder);
        Assert.Equal(EStatus.ZRUSENA, updatedOrder.StatusOrder);

        // Act 2: Attempting to cancel an already-cancelled order must fail
        var response2 = await client.PostAsJsonAsync("/guest/cancel-order-by-token", new CapabilityTokenRequest
        {
            Token = rawToken
        });

        // Assert 2: Rejected
        Assert.Equal(HttpStatusCode.NotFound, response2.StatusCode);
    }

    [Fact]
    public async Task GuestOrderFollow_ExpiredToken_ReturnsNotFound()
    {
        // Arrange: Create order with expired follow token
        var rawFollowToken = CapabilityToken.Create();
        var hashedFollowToken = CapabilityToken.Hash(rawFollowToken);
        var orderId = new Random().Next(100000, 999999);

        var db = _factory.GetDbContext();
        var order = new Order
        {
            Id = orderId,
            UserId = Guid.NewGuid(),
            StatusOrder = EStatus.PRIJATA,
            FollowToken = hashedFollowToken,
            FollowTokenExpiresAt = DateTime.UtcNow.AddDays(-1), // Expired
            CreatedAt = DateTime.UtcNow.AddDays(-181),
            Customizations = new List<Guid>()
        };
        await db.Orders.InsertOneAsync(order);

        var client = _factory.CreateAnonymousClient();

        // Act
        var response = await client.PostAsJsonAsync("/guest/follow-order", new CapabilityTokenRequest
        {
            Token = rawFollowToken
        });

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
