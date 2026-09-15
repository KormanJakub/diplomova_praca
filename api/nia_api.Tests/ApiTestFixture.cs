using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using Mongo2Go;
using Moq;
using nia_api.Data;
using nia_api.Domain.Payments;
using nia_api.Models;
using nia_api.Security;
using nia_api.Services;

namespace nia_api.Tests;

public class ApiWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private static readonly Lazy<MongoDbRunner> RunnerInstance = new(() => MongoDbRunner.Start());

    public const string TestJwtKey = "test-jwt-secret-key-with-at-least-32-chars-long!";
    public const string TestIssuer = "http://localhost";
    public const string TestAudience = "http://localhost";
    public const string AllowedWebOrigin = "https://waffl-e1c23.web.app";

    public string DatabaseName { get; } = $"test_db_{Guid.NewGuid():N}";
    public string ConnectionString => RunnerInstance.Value.ConnectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["NiaDbSettings:ConnectionString"] = ConnectionString,
                ["NiaDbSettings:DatabaseName"] = DatabaseName,
                ["JwtConfig:Key"] = TestJwtKey,
                ["JwtConfig:Issuer"] = TestIssuer,
                ["JwtConfig:Audience"] = TestAudience,
                ["Hosting:Web-Url"] = AllowedWebOrigin,
                ["Hosting:KnownProxies:0"] = "127.0.0.1",
                ["Stripe:SecretKey"] = "sk_test_placeholder_key",
                ["Smtp:Host"] = "localhost",
                ["Smtp:Port"] = "25"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            var emailMock = new Mock<IEmailSender>();
            emailMock.Setup(m => m.SendEmailAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<nia_api.Enums.EEmail>()))
                .Returns(Task.CompletedTask);
            services.RemoveAll<IEmailSender>();
            services.AddSingleton(emailMock.Object);

            services.RemoveAll<IPaymentGateway>();
            services.AddSingleton(PaymentGatewayMock.Object);
        });
    }

    public Mock<IPaymentGateway> PaymentGatewayMock { get; } = new();

    public async Task InitializeAsync()
    {
        _ = RunnerInstance.Value;
        await Task.CompletedTask;
    }

    public new async Task DisposeAsync()
    {
        try
        {
            var client = new MongoClient(ConnectionString);
            await client.DropDatabaseAsync(DatabaseName);
        }
        catch
        {
            // Ignore cleanup errors
        }

        await base.DisposeAsync();
    }

    public NiaDbContext GetDbContext()
    {
        return Services.GetRequiredService<NiaDbContext>();
    }

    public async Task<User> SeedUserAsync(bool isAdmin = false, int tokenVersion = 1, bool isEmailConfirmed = true, string? email = null)
    {
        var db = GetDbContext();
        var passwordService = Services.GetRequiredService<PasswordService>();
        var userEmail = email ?? $"user_{Guid.NewGuid():N}@test.com";

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = userEmail,
            NormalizedEmail = EmailNormalizer.Normalize(userEmail),
            FirstName = "Test",
            LastName = "User",
            Password = passwordService.HashPassword("ValidPassword123!"),
            IsAdmin = isAdmin,
            IsEmailConfirmed = isEmailConfirmed,
            TokenVersion = tokenVersion
        };

        await db.Users.InsertOneAsync(user);
        return user;
    }

    public string GenerateToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(TestJwtKey);

        var claims = new List<Claim>
        {
            new("UserId", user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}".Trim()),
            new("TokenVersion", user.TokenVersion.ToString())
        };

        if (user.IsAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, "admin"));
            claims.Add(new Claim("Role", "admin"));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = TestIssuer,
            Audience = TestAudience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public HttpClient CreateAnonymousClient()
    {
        return CreateClient();
    }

    public HttpClient CreateAuthenticatedClient(User user, string? origin = AllowedWebOrigin)
    {
        var client = CreateClient();
        var token = GenerateToken(user);
        client.DefaultRequestHeaders.Add("Cookie", $"{AuthCookie.Name}={token}");

        if (!string.IsNullOrEmpty(origin))
        {
            client.DefaultRequestHeaders.Add("Origin", origin);
        }

        return client;
    }
}
