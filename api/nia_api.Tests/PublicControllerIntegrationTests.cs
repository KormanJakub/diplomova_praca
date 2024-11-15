using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using MongoDB.Driver;
using Moq;
using Newtonsoft.Json.Linq;
using nia_api.Controllers;
using nia_api.Data;
using nia_api.Models;
using nia_api.Requests;
using nia_api.Services;

namespace nia_api.Tests;

public class PublicControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly IMongoCollection<User> _users;
    private readonly PublicController _controller;
    private readonly PasswordService _passwordService;
    private readonly JwtTokenService _tokenService;
    private readonly IEmailSender _emailSender;
    private readonly NiaDbContext _dbContext;

    public PublicControllerIntegrationTests()
    {
        var settings = new NiaDbSettings
        {
            ConnectionString = "mongodb+srv://admin:CV_1234_2001_PW@niadb.nfhrxz8.mongodb.net/?retryWrites=true&w=majority&appName=niadb",
            DatabaseName = "niadb"
        };

        var options = Options.Create(settings);
        _dbContext = new NiaDbContext(options);
        _users = _dbContext.Users;
        
        var inMemorySettings = new Dictionary<string, string> {
            { "JwtConfig:Key", "bx6VLvhgpXytr92qbx6VLvhgpXytr92q" },
            { "JwtConfig:Issuer", "http://localhost" },
            { "JwtConfig:Audience", "http://localhost" }
        };
        
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _tokenService = new JwtTokenService(configuration);
        _passwordService = new PasswordService();
        _emailSender = new Mock<IEmailSender>().Object;
        
        _controller = new PublicController(_dbContext, _passwordService, _tokenService, _emailSender);
    }

    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenBodyIsEmpty()
    {
        // Arrange
        var expectedResult = "User is empty!";

        // Act
        var result = await _controller.Register(null);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var responseValue = JObject.FromObject(badRequestResult.Value);
        Assert.Equal(expectedResult, (string)responseValue["error"]);
    }
    
    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenEmailIsAlreadyRegistered()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Email = "k.jakub@waffle.com",
            FirstName = "Test",
            LastName = "NUnit",
            Password = "Testing_app_1234",
            RepeatPassword = "Testing_app_1234"
        };
        var expectedResult = "Email is already registered!";
        
        // Act
        var result = await _controller.Register(registerRequest);
        
        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var responseValue = JObject.FromObject(badRequestResult.Value);
        Assert.Equal(expectedResult, (string)responseValue["error"]);
    }
    
    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenFirstNameIsEmpty()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Email = "n-unit@waffle.com",
            FirstName = null,
            LastName = "NUnit",
            Password = "Testing_app_1234",
            RepeatPassword = "Testing_app_1234"
        };
        var expectedResult = "First name is empty!";
        
        // Act
        var result = await _controller.Register(registerRequest);
        
        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var responseValue = JObject.FromObject(badRequestResult.Value);
        Assert.Equal(expectedResult, (string)responseValue["error"]);
    }
    
    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenLastNameIsEmpty()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Email = "n-unit@waffle.com",
            FirstName = "Test",
            LastName = null,
            Password = "Testing_app_1234",
            RepeatPassword = "Testing_app_1234"
        };
        var expectedResult = "Last name is empty!";
        
        // Act
        var result = await _controller.Register(registerRequest);
        
        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var responseValue = JObject.FromObject(badRequestResult.Value);
        Assert.Equal(expectedResult, (string)responseValue["error"]);
    }
    
    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenPasswordIsTooShort()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Email = "n-unit@waffle.com",
            FirstName = "Test",
            LastName = "NUnit",
            Password = "test",
            RepeatPassword = "test"
        };
        var expectedResult = "Password is too short! Min 6 Length";
        
        // Act
        var result = await _controller.Register(registerRequest);
        
        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var responseValue = JObject.FromObject(badRequestResult.Value);
        Assert.Equal(expectedResult, (string)responseValue["error"]);
    }
    
    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenPasswordDoesntContainOneUppercaseLetter()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Email = "n-unit@waffle.com",
            FirstName = "Test",
            LastName = "NUnit",
            Password = "testing_app_1234",
            RepeatPassword = "testing_app_1234"
        };
        var expectedResult = "Password must contain at least one uppercase letter!";
        
        // Act
        var result = await _controller.Register(registerRequest);
        
        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var responseValue = JObject.FromObject(badRequestResult.Value);
        Assert.Equal(expectedResult, (string)responseValue["error"]);
    }
    
    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenPasswordDoesntContainOneLowercaseLetter()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Email = "n-unit@waffle.com",
            FirstName = "Test",
            LastName = "NUnit",
            Password = "TESTING_APP_1234",
            RepeatPassword = "TESTING_APP_1234"
        };
        var expectedResult = "Password must contain at least one lowercase letter!";
        
        // Act
        var result = await _controller.Register(registerRequest);
        
        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var responseValue = JObject.FromObject(badRequestResult.Value);
        Assert.Equal(expectedResult, (string)responseValue["error"]);
    }
    
    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenPasswordsAreNotSame()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Email = "n-unit@waffle.com",
            FirstName = "Test",
            LastName = "NUnit",
            Password = "Testing_app_1234",
            RepeatPassword = "testing_app_1234"
        };
        var expectedResult = "Password's are not same!";
        
        // Act
        var result = await _controller.Register(registerRequest);
        
        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var responseValue = JObject.FromObject(badRequestResult.Value);
        Assert.Equal(expectedResult, (string)responseValue["error"]);
    }
    
    [Fact]
    public async Task Register_ShouldOkResult_SuccessfulRegistered()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Email = "n-unit-test@waffle.com",
            FirstName = "Test",
            LastName = "NUnit",
            Password = "Testing_app_1234",
            RepeatPassword = "Testing_app_1234"
        };
        var expectedResult = "Register successful and verification email sent successfully!";
        
        // Act
        var result = await _controller.Register(registerRequest);
        
        // Assert
        var badRequestResult = Assert.IsType<OkObjectResult>(result);
        var responseValue = JObject.FromObject(badRequestResult.Value);
        Assert.Equal(expectedResult, (string)responseValue["message"]);
        
        // CleanUp
        await _users.DeleteOneAsync(u => u.Email == registerRequest.Email);
    }
    
    [Fact]
    public async Task Login_ShouldReturnBadRequest_WhenBodyIsNull()
    {
        // Arrange
        var expectedRespond = "User Request is null!";
        
        // Act
        var result = await _controller.Login(null);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var responseValue = JObject.FromObject(badRequestResult.Value);
        Assert.Equal(expectedRespond, (string)responseValue["error"]);
    }
    
    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenUserIsNotRegistered()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "no-email-registered@waffle.com",
            Password = "wrongPassword"
        };
        var expectedRespond = "User is not registered!";
        
        // Act
        var result = await _controller.Login(loginRequest);
        
        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var responseValue = JObject.FromObject(unauthorizedResult.Value);
        Assert.Equal(expectedRespond, (string)responseValue["error"]);
    }
    
    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenPasswordDoesNotMatch()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "k.jakub@waffle.com",
            Password = "wrongPassword"
        };
        var expectedRespond = "Password's do not match!";

        // Act
        var result = await _controller.Login(loginRequest);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var responseValue = JObject.FromObject(unauthorizedResult.Value);
        Assert.Equal(expectedRespond, (string)responseValue["error"]);
    }
    
    [Fact]
    public async Task Login_ShouldReturnTokenAndEmailConfirmation_WhenEmailConfirmationIsFalse()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "k.jakub@waffle.com",
            Password = "Nepoviem_1234"
        };
        var expectedFalseEmailConfirmation = false;

        // Act
        var result = await _controller.Login(loginRequest);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);

        var responseValue = JObject.FromObject(okResult.Value);

        Assert.NotNull(responseValue["token"]);
        Assert.IsType<string>((string)responseValue["token"]);
        Assert.Equal(expectedFalseEmailConfirmation, (bool)responseValue["email_confirmation"]);
    }
    
    [Fact]
    public async Task Login_ShouldOkResult_WhenAdminIsLogged()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "k.jakub@waffle.com",
            Password = "Nepoviem_1234"
        };
        var expectedAdminResult = "admin";

        // Act
        var result = await _controller.Login(loginRequest);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);

        var responseValue = JObject.FromObject(okResult.Value);

        Assert.NotNull(responseValue["token"]);
        Assert.IsType<string>((string)responseValue["token"]);
        Assert.Equal(expectedAdminResult, (string)responseValue["role"]);
    }

    [Fact]
    public async Task ForgotPassword_ShouldUnauthorized_WhenUserIsNotRegistered()
    {
        // Arrange
        var email = "not-registered-user@waffl.com";
        var expectedResult = "User is not registered!";

        // Act
        var result = await _controller.ForgotPassword(email);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var responseValue = JObject.FromObject(unauthorizedResult.Value);
        Assert.Equal(expectedResult, (string)responseValue["error"]);
    }
    
    [Fact]
    public async Task ForgotPassword_ShouldOkResult()
    {
        // Arrange
        var email = "k.jakub@waffle.com";
        var expectedResult = "Verification code for Forgot Password sent successfully.!";

        // Act
        var result = await _controller.ForgotPassword(email);

        // Assert
        var okObjectResult = Assert.IsType<OkObjectResult>(result);
        var responseValue = JObject.FromObject(okObjectResult.Value);
        Assert.Equal(expectedResult, (string)responseValue["message"]);
    }
    
    [Fact]
    public async Task VerificationCode_ShouldUnauthorized_WhenUserIsNotRegistered()
    {
        // Arrange
        var verificationCodeRequest = new VerificateCodeRequest()
        {
            Email = "not-registered-user@waffl.com",
            VerificationCode = 0
        };
        var expectedResult = "User is not registered!";

        // Act
        var result = await _controller.VerificateCode(verificationCodeRequest);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var responseValue = JObject.FromObject(unauthorizedResult.Value);
        Assert.Equal(expectedResult, (string)responseValue["error"]);
    }
    
    [Fact]
    public async Task VerificationCode_ShouldBadRequest_WhenCodeIsLarge()
    {
        // Arrange
        var verificationCodeRequest = new VerificateCodeRequest()
        {
            Email = "k.jakub@waffle.com",
            VerificationCode = 1000000
        };
        var expectedResult = "Wrong verification code!";

        // Act
        var result = await _controller.VerificateCode(verificationCodeRequest);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var responseValue = JObject.FromObject(badRequestResult.Value);
        Assert.Equal(expectedResult, (string)responseValue["error"]);
    }
    
    [Fact]
    public async Task VerificationCode_ShouldBadRequest_WhenEnteredBadCode()
    {
        // Arrange
        var verificationCodeRequest = new VerificateCodeRequest()
        {
            Email = "test_of_ver@test.com",
            VerificationCode = 532563
        };
        var expectedResult = "You entered bad code";

        // Act
        var result = await _controller.VerificateCode(verificationCodeRequest);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var responseValue = JObject.FromObject(badRequestResult.Value);
        Assert.Equal(expectedResult, (string)responseValue["error"]);
    }
    
    [Fact]
    public async Task VerificationCode_ShouldOkResult()
    {
        // Arrange
        var verificationCodeRequest = new VerificateCodeRequest()
        {
            Email = "k.jakub@nia.com",
            VerificationCode = 532564
        };
        var expectedResult = "You entered good code! Write new password!";

        // Act
        var result = await _controller.VerificateCode(verificationCodeRequest);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var responseValue = JObject.FromObject(okResult.Value);
        Assert.Equal(expectedResult, (string)responseValue["message"]);
    }
    
    [Fact]
    public async Task NewVerificationCode_ShouldUnauthorized_WhenUserIsNotRegistered()
    {
        // Arrange
        var emailRequest = new EmailRequest()
        {
            Email = "not-registered-user@waffl.com",
        };
        var expectedResult = "User is not registered!";

        // Act
        var result = await _controller.NewVerificateCode(emailRequest);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var responseValue = JObject.FromObject(unauthorizedResult.Value);
        Assert.Equal(expectedResult, (string)responseValue["error"]);
    }
    
    [Fact]
    public async Task NewVerificationCode_ShouldOkResult()
    {
        // Arrange
        var emailRequest = new EmailRequest()
        {
            Email = "no_email@nia.com",
        };
        var expectedResult = "You should receive new verification code!";

        // Act
        var result = await _controller.NewVerificateCode(emailRequest);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var responseValue = JObject.FromObject(okResult.Value);
        Assert.NotEqual(expectedResult, (string)responseValue["message"]);
    }
}