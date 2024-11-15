using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using MongoDB.Driver;
using Moq;
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

    public PublicControllerIntegrationTests()
    {
        var client = new MongoClient("");
    }
}