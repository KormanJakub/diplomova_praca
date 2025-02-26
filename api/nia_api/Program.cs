using System.Text;
using System.Text.Json;
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
            Encoding.UTF8
                .GetBytes(builder.Configuration["JwtConfig:Key"])
        ),
        
        ValidateIssuer = false,
        
        ValidateAudience = false,
        
        ClockSkew = TimeSpan.FromMinutes(5),
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

var corsPolicy = "AllowAll";

builder.Services.AddCors(options =>
{
    options.AddPolicy(corsPolicy, policy =>
    {
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<NiaDbSettings>(builder.Configuration.GetSection("NiaDbSettings"));
builder.Services.AddControllers()
    .AddJsonOptions(
        options => options.JsonSerializerOptions.PropertyNamingPolicy = null);

builder.Services.AddTransient<IEmailSender, EmailSenderService>();
builder.Services.AddSingleton<NiaDbContext>();
builder.Services.AddSingleton<LocalTimeService>();
builder.Services.AddSingleton<HeaderReaderService>();
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddSingleton<PasswordService>();

builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));
StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

app.UseStaticFiles(); 

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.UseMiddleware<UserMiddleware>();

//app.UseMiddleware<AdminMiddleware>();

app.UseCors(corsPolicy);

app.MapControllers();

app.MapGet("/", () => "Health endpoint");

app.Run();
