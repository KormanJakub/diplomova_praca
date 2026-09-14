using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using nia_api.Data;
using nia_api.Middleware;
using nia_api.Models;
using nia_api.Services;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = true;
    x.SaveToken = false;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["JwtConfig:Key"])
        ),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["JwtConfig:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["JwtConfig:Audience"],
        RoleClaimType = "Role",
        ClockSkew = TimeSpan.FromMinutes(1),
    };

    x.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError("Authentication failed.", context.Exception);
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Token validated.");
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            var result = JsonSerializer.Serialize(new { error = "User is not logged in" });
            return context.Response.WriteAsync(result);
        }
    };
});


var corsPolicy = "WafflWeb";
var webOrigin = builder.Configuration["Hosting:Web-Url"]?.TrimEnd('/');
builder.Services.AddCors(options =>
{
    options.AddPolicy(corsPolicy, policy =>
    {
        var origins = new List<string>();
        if (!string.IsNullOrWhiteSpace(webOrigin)) origins.Add(webOrigin);
        if (builder.Environment.IsDevelopment()) origins.Add("http://localhost:4200");
        policy.WithOrigins(origins.ToArray()).AllowAnyHeader().AllowAnyMethod();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<NiaDbSettings>(builder.Configuration.GetSection("NiaDbSettings"));
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null);

builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("sensitive", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});

builder.Services.AddTransient<IEmailSender, EmailSenderService>();
builder.Services.AddSingleton<NiaDbContext>();
builder.Services.AddSingleton<LocalTimeService>();
builder.Services.AddSingleton<HeaderReaderService>();
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddSingleton<PasswordService>();

builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));
StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();

app.UseCors(corsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<UserMiddleware>();

app.MapControllers();
app.MapGet("/", () => "Health endpoint");

app.Run();
