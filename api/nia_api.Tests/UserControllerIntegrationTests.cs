using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MongoDB.Driver;
using nia_api.Enums;
using nia_api.Models;
using nia_api.Security;

namespace nia_api.Tests;

public class UserControllerIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public UserControllerIntegrationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetProfile_WithValidSession_ReturnsUserDetails()
    {
        // Arrange
        var user = await _factory.SeedUserAsync(isAdmin: false);
        var client = _factory.CreateAuthenticatedClient(user);

        // Act
        var response = await client.GetAsync("/user/profile");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(user.Email, profile.GetProperty("Email").GetString());
        Assert.Equal(user.FirstName, profile.GetProperty("FirstName").GetString());
        Assert.Equal(user.LastName, profile.GetProperty("LastName").GetString());
    }

    [Fact]
    public async Task GetProfile_Anonymous_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateAnonymousClient();

        // Act
        var response = await client.GetAsync("/user/profile");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetOrders_WithValidSession_ReturnsUserOrders()
    {
        // Arrange: Seed user and an order belonging to them
        var user = await _factory.SeedUserAsync(isAdmin: false);
        var client = _factory.CreateAuthenticatedClient(user);

        var orderId = new Random().Next(100000, 999999);
        var db = _factory.GetDbContext();
        var order = new Order
        {
            Id = orderId,
            UserId = user.Id,
            StatusOrder = EStatus.PRIJATA,
            CancellationToken = CapabilityToken.Hash(CapabilityToken.Create()),
            CancellationTokenExpiresAt = DateTime.UtcNow.AddHours(24),
            FollowToken = CapabilityToken.Hash(CapabilityToken.Create()),
            FollowTokenExpiresAt = DateTime.UtcNow.AddDays(180),
            TotalPrice = 49.99m,
            CreatedAt = DateTime.UtcNow,
            Customizations = new List<Guid>()
        };
        await db.Orders.InsertOneAsync(order);

        // Act
        var response = await client.GetAsync("/user/orders");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(content.TryGetProperty("orders", out var ordersArray));
        Assert.Equal(JsonValueKind.Array, ordersArray.ValueKind);
        Assert.True(ordersArray.GetArrayLength() > 0);
    }

    [Fact]
    public async Task CancelOrder_ForOwnOrder_CancelsSuccessfully()
    {
        // Arrange: Seed user and their order
        var user = await _factory.SeedUserAsync(isAdmin: false);
        var client = _factory.CreateAuthenticatedClient(user);

        var orderId = new Random().Next(100000, 999999);
        var db = _factory.GetDbContext();
        var order = new Order
        {
            Id = orderId,
            UserId = user.Id,
            StatusOrder = EStatus.PRIJATA,
            CancellationToken = CapabilityToken.Hash(CapabilityToken.Create()),
            CancellationTokenExpiresAt = DateTime.UtcNow.AddHours(24),
            FollowToken = CapabilityToken.Hash(CapabilityToken.Create()),
            FollowTokenExpiresAt = DateTime.UtcNow.AddDays(180),
            CreatedAt = DateTime.UtcNow,
            Customizations = new List<Guid>()
        };
        await db.Orders.InsertOneAsync(order);

        // Act: POST /user/cancel-order/{orderId}
        var response = await client.PostAsync($"/user/cancel-order/{orderId}", null);

        // Assert: 200 OK
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify order is now ZRUSENA in database
        var updated = await db.Orders.Find(o => o.Id == orderId).FirstOrDefaultAsync();
        Assert.NotNull(updated);
        Assert.Equal(EStatus.ZRUSENA, updated.StatusOrder);
    }

    [Fact]
    public async Task CancelOrder_ForAnotherUsersOrder_ReturnsNotFound()
    {
        // Arrange: Order belongs to user1, but user2 attempts to cancel it
        var user1 = await _factory.SeedUserAsync(isAdmin: false);
        var user2 = await _factory.SeedUserAsync(isAdmin: false);
        var client2 = _factory.CreateAuthenticatedClient(user2);

        var orderId = new Random().Next(100000, 999999);
        var db = _factory.GetDbContext();
        var order = new Order
        {
            Id = orderId,
            UserId = user1.Id,
            StatusOrder = EStatus.PRIJATA,
            CancellationToken = CapabilityToken.Hash(CapabilityToken.Create()),
            CancellationTokenExpiresAt = DateTime.UtcNow.AddHours(24),
            FollowToken = CapabilityToken.Hash(CapabilityToken.Create()),
            FollowTokenExpiresAt = DateTime.UtcNow.AddDays(180),
            CreatedAt = DateTime.UtcNow,
            Customizations = new List<Guid>()
        };
        await db.Orders.InsertOneAsync(order);

        // Act: user2 tries to cancel user1's order
        var response = await client2.PostAsync($"/user/cancel-order/{orderId}", null);

        // Assert: CancelByUserAsync filter checks UserId == user2.Id, so not found
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        // Verify order was NOT cancelled
        var orderInDb = await db.Orders.Find(o => o.Id == orderId).FirstOrDefaultAsync();
        Assert.NotNull(orderInDb);
        Assert.Equal(EStatus.PRIJATA, orderInDb.StatusOrder);
    }
}