using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Moq;
using Newtonsoft.Json.Linq;
using nia_api.Controllers;
using nia_api.Data;
using nia_api.Models;
using nia_api.Requests;
using nia_api.Security;
using nia_api.Services;

namespace nia_api.Tests;

public class PublicControllerIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly NiaDbContext _dbContext;
    private readonly IMongoCollection<User> _users;
    private readonly PasswordService _passwordService;
    private readonly JwtTokenService _tokenService;
    private readonly IEmailSender _emailSender;
    private readonly PublicController _controller;

    public PublicControllerIntegrationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _dbContext = factory.GetDbContext();
        _users = _dbContext.Users;

        _passwordService = new PasswordService();
        _tokenService = new JwtTokenService(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtConfig:Key"] = ApiWebApplicationFactory.TestJwtKey,
                ["JwtConfig:Issuer"] = ApiWebApplicationFactory.TestIssuer,
                ["JwtConfig:Audience"] = ApiWebApplicationFactory.TestAudience
            })
            .Build());

        var emailMock = new Mock<IEmailSender>();
        emailMock.Setup(m => m.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<nia_api.Enums.EEmail>()))
            .Returns(Task.CompletedTask);
        _emailSender = emailMock.Object;

        _controller = new PublicController(_dbContext, _passwordService, _tokenService, _emailSender)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenBodyIsEmpty()
    {
        var result = await _controller.Register(null!);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var responseValue = JObject.FromObject(badRequestResult.Value!);
        Assert.Equal("User is empty!", (string?)responseValue["error"]);
    }

    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenEmailIsAlreadyRegistered()
    {
        // Arrange: dynamically seed an existing user
        var existing = await _factory.SeedUserAsync(email: $"registered_{Guid.NewGuid():N}@test.com");

        var registerRequest = new RegisterRequest
        {
            Email = existing.Email,
            FirstName = "Test",
            LastName = "User",
            Password = "Testing_app_1234",
            RepeatPassword = "Testing_app_1234"
        };

        // Act
        var result = await _controller.Register(registerRequest);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var responseValue = JObject.FromObject(badRequestResult.Value!);
        Assert.Equal("Email is already registered!", (string?)responseValue["error"]);
    }

    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenFirstNameIsEmpty()
    {
        var registerRequest = new RegisterRequest
        {
            Email = $"user_{Guid.NewGuid():N}@test.com",
            FirstName = null!,
            LastName = "User",
            Password = "Testing_app_1234",
            RepeatPassword = "Testing_app_1234"
        };

        var result = await _controller.Register(registerRequest);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var responseValue = JObject.FromObject(badRequestResult.Value!);
        Assert.Equal("First name is empty!", (string?)responseValue["error"]);
    }

    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenLastNameIsEmpty()
    {
        var registerRequest = new RegisterRequest
        {
            Email = $"user_{Guid.NewGuid():N}@test.com",
            FirstName = "Test",
            LastName = null!,
            Password = "Testing_app_1234",
            RepeatPassword = "Testing_app_1234"
        };

        var result = await _controller.Register(registerRequest);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var responseValue = JObject.FromObject(badRequestResult.Value!);
        Assert.Equal("Last name is empty!", (string?)responseValue["error"]);
    }

    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenPasswordIsTooShort()
    {
        var registerRequest = new RegisterRequest
        {
            Email = $"user_{Guid.NewGuid():N}@test.com",
            FirstName = "Test",
            LastName = "User",
            Password = "short",
            RepeatPassword = "short"
        };

        var result = await _controller.Register(registerRequest);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var responseValue = JObject.FromObject(badRequestResult.Value!);
        Assert.Equal("Password is too short! Minimum length is 8.", (string?)responseValue["error"]);
    }

    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenPasswordDoesntContainOneUppercaseLetter()
    {
        var registerRequest = new RegisterRequest
        {
            Email = $"user_{Guid.NewGuid():N}@test.com",
            FirstName = "Test",
            LastName = "User",
            Password = "testing_app_1234",
            RepeatPassword = "testing_app_1234"
        };

        var result = await _controller.Register(registerRequest);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var responseValue = JObject.FromObject(badRequestResult.Value!);
        Assert.Equal("Password must contain at least one uppercase letter!", (string?)responseValue["error"]);
    }

    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenPasswordDoesntContainOneLowercaseLetter()
    {
        var registerRequest = new RegisterRequest
        {
            Email = $"user_{Guid.NewGuid():N}@test.com",
            FirstName = "Test",
            LastName = "User",
            Password = "TESTING_APP_1234",
            RepeatPassword = "TESTING_APP_1234"
        };

        var result = await _controller.Register(registerRequest);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var responseValue = JObject.FromObject(badRequestResult.Value!);
        Assert.Equal("Password must contain at least one lowercase letter!", (string?)responseValue["error"]);
    }

    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenPasswordsAreNotSame()
    {
        var registerRequest = new RegisterRequest
        {
            Email = $"user_{Guid.NewGuid():N}@test.com",
            FirstName = "Test",
            LastName = "User",
            Password = "Testing_app_1234",
            RepeatPassword = "Different_password_1234"
        };

        var result = await _controller.Register(registerRequest);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var responseValue = JObject.FromObject(badRequestResult.Value!);
        Assert.Equal("Password's are not same!", (string?)responseValue["error"]);
    }

    [Fact]
    public async Task Register_ShouldReturnOk_WhenRegistrationIsValid()
    {
        var email = $"new_user_{Guid.NewGuid():N}@test.com";
        var registerRequest = new RegisterRequest
        {
            Email = email,
            FirstName = "Test",
            LastName = "User",
            Password = "Testing_app_1234",
            RepeatPassword = "Testing_app_1234"
        };

        var result = await _controller.Register(registerRequest);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var responseValue = JObject.FromObject(okResult.Value!);
        Assert.Equal("Register successful and verification email sent successfully!", (string?)responseValue["message"]);

        // Cleanup
        await _users.DeleteOneAsync(u => u.Email == email);
    }

    [Fact]
    public async Task Login_ShouldReturnBadRequest_WhenBodyIsNull()
    {
        var result = await _controller.Login(null!);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var responseValue = JObject.FromObject(badRequestResult.Value!);
        Assert.Equal("User Request is null!", (string?)responseValue["error"]);
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenUserIsNotRegistered()
    {
        var loginRequest = new LoginRequest
        {
            Email = $"nonexistent_{Guid.NewGuid():N}@test.com",
            Password = "AnyPassword123!"
        };

        var result = await _controller.Login(loginRequest);

        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var responseValue = JObject.FromObject(unauthorizedResult.Value!);
        // Anti-enumeration: returns generic Invalid credentials.
        Assert.Equal("Invalid credentials.", (string?)responseValue["error"]);
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenPasswordDoesNotMatch()
    {
        var user = await _factory.SeedUserAsync(isAdmin: false);
        var loginRequest = new LoginRequest
        {
            Email = user.Email,
            Password = "WrongPassword123!"
        };

        var result = await _controller.Login(loginRequest);

        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var responseValue = JObject.FromObject(unauthorizedResult.Value!);
        Assert.Equal("Invalid credentials.", (string?)responseValue["error"]);
    }

    [Fact]
    public async Task Login_ShouldReturnForbidden_WhenEmailIsNotConfirmed()
    {
        var user = await _factory.SeedUserAsync(isAdmin: false, isEmailConfirmed: false);
        var loginRequest = new LoginRequest
        {
            Email = user.Email,
            Password = "ValidPassword123!"
        };

        var result = await _controller.Login(loginRequest);

        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, statusCodeResult.StatusCode);
        var responseValue = JObject.FromObject(statusCodeResult.Value!);
        Assert.Equal("Email is not confirmed.", (string?)responseValue["error"]);
    }

    [Fact]
    public async Task Login_ShouldReturnOk_WhenCredentialsAreValid()
    {
        var user = await _factory.SeedUserAsync(isAdmin: false, isEmailConfirmed: true);
        var loginRequest = new LoginRequest
        {
            Email = user.Email,
            Password = "ValidPassword123!"
        };

        var result = await _controller.Login(loginRequest);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var responseValue = JObject.FromObject(okResult.Value!);
        Assert.Equal("user", (string?)responseValue["role"]);
        Assert.Equal(user.FirstName, (string?)responseValue["firstName"]);
        Assert.True((bool?)responseValue["email_confirmation"]);
    }

    [Fact]
    public async Task Login_ShouldReturnOkWithAdminRole_WhenAdminLogsIn()
    {
        var admin = await _factory.SeedUserAsync(isAdmin: true, isEmailConfirmed: true);
        var loginRequest = new LoginRequest
        {
            Email = admin.Email,
            Password = "ValidPassword123!"
        };

        var result = await _controller.Login(loginRequest);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var responseValue = JObject.FromObject(okResult.Value!);
        Assert.Equal("admin", (string?)responseValue["role"]);
    }

    [Fact]
    public async Task ForgotPassword_ShouldReturnSafeMessage_WhetherUserExistsOrNot()
    {
        // Anti-enumeration: returns same safe message regardless
        var nonExistentEmail = $"nonexistent_{Guid.NewGuid():N}@test.com";
        var result1 = await _controller.ForgotPassword(nonExistentEmail);
        var ok1 = Assert.IsType<OkObjectResult>(result1);
        var val1 = JObject.FromObject(ok1.Value!);
        Assert.Equal("Ak účet existuje, poslali sme pokyny na obnovu hesla.", (string?)val1["message"]);

        var existingUser = await _factory.SeedUserAsync();
        var result2 = await _controller.ForgotPassword(existingUser.Email!);
        var ok2 = Assert.IsType<OkObjectResult>(result2);
        var val2 = JObject.FromObject(ok2.Value!);
        Assert.Equal("Ak účet existuje, poslali sme pokyny na obnovu hesla.", (string?)val2["message"]);
    }

    [Fact]
    public async Task VerificationCode_ShouldReturnBadRequest_WhenCodeIsOutOfRange()
    {
        var user = await _factory.SeedUserAsync();
        var request = new VerificateCodeRequest
        {
            Email = user.Email,
            VerificationCode = 99999 // Must be 6 digits (100000 - 999999)
        };

        var result = await _controller.VerificateCode(request);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var responseValue = JObject.FromObject(badRequestResult.Value!);
        Assert.Equal("Invalid verification code.", (string?)responseValue["error"]);
    }

    [Fact]
    public async Task VerificationCode_ShouldReturnBadRequest_WhenCodeDoesNotMatch()
    {
        var user = await _factory.SeedUserAsync();
        // Set a valid unexpired verification code in DB
        await _users.UpdateOneAsync(
            u => u.Id == user.Id,
            Builders<User>.Update
                .Set(u => u.VerificationCode, 555555)
                .Set(u => u.VerificationCodeExpiresAt, DateTime.UtcNow.AddMinutes(10)));

        var request = new VerificateCodeRequest
        {
            Email = user.Email,
            VerificationCode = 111111 // Wrong code
        };

        var result = await _controller.VerificateCode(request);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var responseValue = JObject.FromObject(badRequestResult.Value!);
        Assert.Equal("Invalid verification code.", (string?)responseValue["error"]);
    }

    [Fact]
    public async Task VerificationCode_ShouldReturnOk_WhenCodeIsValid()
    {
        var user = await _factory.SeedUserAsync();
        const int validCode = 654321;
        await _users.UpdateOneAsync(
            u => u.Id == user.Id,
            Builders<User>.Update
                .Set(u => u.VerificationCode, validCode)
                .Set(u => u.VerificationCodeExpiresAt, DateTime.UtcNow.AddMinutes(10)));

        var request = new VerificateCodeRequest
        {
            Email = user.Email,
            VerificationCode = validCode
        };

        var result = await _controller.VerificateCode(request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var responseValue = JObject.FromObject(okResult.Value!);
        Assert.Equal("You entered good code! Write new password!", (string?)responseValue["message"]);
    }

    [Fact]
    public async Task NewVerificationCode_ShouldReturnSafeMessage_WhetherUserExistsOrNot()
    {
        // Anti-enumeration: returns same safe message
        var nonExistentEmail = $"unknown_{Guid.NewGuid():N}@test.com";
        var result1 = await _controller.NewVerificationCode(new EmailRequest { Email = nonExistentEmail });
        var ok1 = Assert.IsType<OkObjectResult>(result1);
        var val1 = JObject.FromObject(ok1.Value!);
        Assert.Equal("Ak účet existuje, nový kód bol odoslaný.", (string?)val1["message"]);

        var existingUser = await _factory.SeedUserAsync();
        var result2 = await _controller.NewVerificationCode(new EmailRequest { Email = existingUser.Email! });
        var ok2 = Assert.IsType<OkObjectResult>(result2);
        var val2 = JObject.FromObject(ok2.Value!);
        Assert.Equal("Ak účet existuje, nový kód bol odoslaný.", (string?)val2["message"]);
    }
}
