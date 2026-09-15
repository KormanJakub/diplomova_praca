using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MongoDB.Driver;
using nia_api.Models;
using nia_api.Security;

namespace nia_api.Tests;

public class SecurityAndAccessIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public SecurityAndAccessIntegrationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AdminEndpoint_AnonymousAccess_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateAnonymousClient();

        // Act
        var response = await client.GetAsync("/admin/tag/getAll");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("User is not logged in", content);
    }

    [Fact]
    public async Task AdminEndpoint_CustomerRoleAccess_ReturnsForbidden()
    {
        // Arrange
        var customer = await _factory.SeedUserAsync(isAdmin: false);
        var client = _factory.CreateAuthenticatedClient(customer);

        // Act
        var response = await client.GetAsync("/admin/tag/getAll");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminEndpoint_AdminRoleAccess_IsAllowed()
    {
        // Arrange
        var admin = await _factory.SeedUserAsync(isAdmin: true);
        var client = _factory.CreateAuthenticatedClient(admin);

        // Act
        var response = await client.GetAsync("/admin/tag/getAll");

        // Assert: not 401 or 403 (either 200 or 404 if no tags exist)
        Assert.True(
            response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound,
            $"Expected OK or NotFound, but got {response.StatusCode}");
    }

    [Fact]
    public async Task AdminEndpoint_RevokedSession_ReturnsUnauthorized()
    {
        // Arrange: seed an admin, create client with tokenVersion=1, then increment tokenVersion in DB
        var admin = await _factory.SeedUserAsync(isAdmin: true, tokenVersion: 1);
        var client = _factory.CreateAuthenticatedClient(admin);

        var db = _factory.GetDbContext();
        await db.Users.UpdateOneAsync(
            u => u.Id == admin.Id,
            Builders<User>.Update.Set(u => u.TokenVersion, 2));

        // Act
        var response = await client.GetAsync("/admin/tag/getAll");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UserEndpoint_AnonymousAccess_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateAnonymousClient();

        // Act
        var response = await client.GetAsync("/user/profile");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UserEndpoint_DeletedUser_ReturnsUnauthorizedWithUserDoesNotExist()
    {
        // Arrange: seed user, generate client, but delete user from DB so UserMiddleware rejects
        var user = await _factory.SeedUserAsync(isAdmin: false);
        var client = _factory.CreateAuthenticatedClient(user);

        var db = _factory.GetDbContext();
        await db.Users.DeleteOneAsync(u => u.Id == user.Id);

        // Act
        var response = await client.GetAsync("/user/profile");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UserEndpoint_ValidSession_ReturnsOkWithProfile()
    {
        // Arrange
        var user = await _factory.SeedUserAsync(isAdmin: false);
        var client = _factory.CreateAuthenticatedClient(user);

        // Act
        var response = await client.GetAsync("/user/profile");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(user.Email, json.GetProperty("Email").GetString());
    }

    [Fact]
    public async Task CsrfProtection_AuthenticatedUnsafeRequest_WithUntrustedOrigin_ReturnsForbidden()
    {
        // Arrange
        var user = await _factory.SeedUserAsync(isAdmin: false);
        var client = _factory.CreateAuthenticatedClient(user, origin: "https://attacker-origin.com");

        // Act: POST is an unsafe HTTP method
        var response = await client.PostAsync("/public/logout", null);

        // Assert: Origin check in Program.cs rejects untrusted origins on authenticated requests
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CsrfProtection_AuthenticatedUnsafeRequest_WithTrustedOrigin_IsAllowed()
    {
        // Arrange
        var user = await _factory.SeedUserAsync(isAdmin: false);
        var client = _factory.CreateAuthenticatedClient(user, origin: ApiWebApplicationFactory.AllowedWebOrigin);

        // Act: POST with allowed origin
        var response = await client.PostAsync("/public/logout", null);

        // Assert: Allowed origin passes check (logout returns 204 NoContent)
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
