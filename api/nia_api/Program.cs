using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using nia_api.Data;
using nia_api.Domain.Configuration;
using nia_api.Domain.Orders;
using nia_api.Domain.Payments;
using nia_api.Middleware;
using nia_api.Models;
using nia_api.Security;
using nia_api.Services;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 6 * 1024 * 1024);
builder.Services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = 6 * 1024 * 1024);
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null);

builder.Services.Configure<NiaDbSettings>(builder.Configuration.GetSection("NiaDbSettings"));
builder.Services.AddSingleton<NiaDbContext>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = true;
    options.SaveToken = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["JwtConfig:Key"]
                ?? throw new InvalidOperationException("Missing JWT signing key."))),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["JwtConfig:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["JwtConfig:Audience"],
        ValidateLifetime = true,
        RoleClaimType = "Role",
        ClockSkew = TimeSpan.FromMinutes(1)
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (string.IsNullOrWhiteSpace(context.Token) &&
                context.Request.Cookies.TryGetValue(AuthCookie.Name, out var cookieToken))
                context.Token = cookieToken;
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("Authentication failed: {ExceptionType}", context.Exception.GetType().Name);
            return Task.CompletedTask;
        },
        OnTokenValidated = async context =>
        {
            var userIdValue = context.Principal?.FindFirst("UserId")?.Value;
            var versionValue = context.Principal?.FindFirst("TokenVersion")?.Value;
            if (!Guid.TryParse(userIdValue, out var userId) || !int.TryParse(versionValue, out var tokenVersion))
            {
                context.Fail("Invalid session.");
                return;
            }

            var db = context.HttpContext.RequestServices.GetRequiredService<NiaDbContext>();
            var dbUser = await db.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();
            var tokenIsAdmin = context.Principal?.IsInRole("admin") == true;
            if (dbUser == null || dbUser.TokenVersion != tokenVersion || tokenIsAdmin != dbUser.IsAdmin)
                context.Fail("Session was revoked.");
        },
        OnChallenge = context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            return context.Response.WriteAsync(JsonSerializer.Serialize(new { error = "User is not logged in" }));
        }
    };
});

var corsPolicy = "WafflWeb";
var webOrigin = builder.Configuration["Hosting:Web-Url"]?.TrimEnd('/');
builder.Services.AddCors(options => options.AddPolicy(corsPolicy, policy =>
{
    var origins = new List<string>();
    if (!string.IsNullOrWhiteSpace(webOrigin)) origins.Add(webOrigin);
    if (builder.Environment.IsDevelopment()) origins.Add("http://localhost:4200");
    policy.WithOrigins(origins.ToArray()).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
}));

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    foreach (var value in builder.Configuration.GetSection("Hosting:KnownProxies").Get<string[]>() ?? [])
        if (IPAddress.TryParse(value, out var address)) options.KnownProxies.Add(address);
});

builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    static string Key(HttpContext context) => context.User.FindFirst("UserId")?.Value ??
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    static FixedWindowRateLimiterOptions Window(int permits) => new()
    {
        PermitLimit = permits,
        Window = TimeSpan.FromMinutes(1),
        QueueLimit = 0,
        AutoReplenishment = true
    };

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(Key(context), _ => Window(120)));
    options.AddPolicy("sensitive", context =>
        RateLimitPartition.GetFixedWindowLimiter(Key(context), _ => Window(10)));
    options.AddPolicy("public-write", context =>
        RateLimitPartition.GetFixedWindowLimiter(Key(context), _ => Window(5)));
    options.AddPolicy("public-read", context =>
        RateLimitPartition.GetFixedWindowLimiter(Key(context), _ => Window(30)));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddTransient<IEmailSender, EmailSenderService>();
builder.Services.AddSingleton<LocalTimeService>();
builder.Services.AddSingleton<HeaderReaderService>();
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddScoped<IMerchantConfigurationService, MerchantConfigurationService>();
builder.Services.AddScoped<IPaymentGateway, StripePaymentGateway>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<IOrderStore, MongoOrderStore>();
builder.Services.AddScoped<IOrderSequenceStore, MongoOrderSequenceStore>();
builder.Services.AddScoped<IOrderLifecycleService, OrderLifecycleService>();
builder.Services.AddSingleton<PasswordService>();
builder.Services.AddHostedService<SecurityDataInitializer>();

builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));
StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseHsts();
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        return Task.CompletedTask;
    });
    await next();
});
app.UseStaticFiles();
app.UseRouting();
app.UseCors(corsPolicy);
app.UseAuthentication();
app.Use(async (context, next) =>
{
    var unsafeMethod = HttpMethods.IsPost(context.Request.Method) || HttpMethods.IsPut(context.Request.Method) ||
                       HttpMethods.IsPatch(context.Request.Method) || HttpMethods.IsDelete(context.Request.Method);
    if (unsafeMethod && context.User.Identity?.IsAuthenticated == true)
    {
        var origin = context.Request.Headers.Origin.ToString().TrimEnd('/');
        if (!string.IsNullOrEmpty(origin) && !string.Equals(origin, webOrigin, StringComparison.OrdinalIgnoreCase) &&
            !(app.Environment.IsDevelopment() && origin == "http://localhost:4200"))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }
    }
    await next();
});
app.UseAuthorization();
app.UseRateLimiter();
app.UseMiddleware<UserMiddleware>();

app.MapControllers();
app.MapGet("/", () => "Health endpoint");

app.Run();

public partial class Program { }
