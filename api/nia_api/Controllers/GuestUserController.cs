using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using nia_api.Data;
using nia_api.Domain.Configuration;
using nia_api.Domain.Orders;
using nia_api.Enums;
using nia_api.Models;
using nia_api.Requests;
using nia_api.Security;
using nia_api.Services;

namespace nia_api.Controllers;

[ApiController]
[Route("guest")]
public class GuestUserController : ControllerBase
{
    private readonly IMongoCollection<Product> _products;
    private readonly IMongoCollection<GuestUser> _guestUsers;
    private readonly IMongoCollection<Design> _designs;
    private readonly IMongoCollection<Customization> _customizations;
    private readonly IOrderStore _orderStore;
    private readonly IOrderLifecycleService _orderLifecycleService;
    private readonly OrderService _orderService;
    private readonly IMerchantConfigurationService _configService;

    public GuestUserController(
        NiaDbContext context,
        OrderService orderService,
        IOrderStore orderStore,
        IOrderLifecycleService orderLifecycleService,
        IMerchantConfigurationService? configService = null)
    {
        _products = context.Products;
        _guestUsers = context.GuestUsers;
        _designs = context.Designs;
        _customizations = context.Customizations;
        _orderStore = orderStore;
        _orderLifecycleService = orderLifecycleService;
        _orderService = orderService;
        _configService = configService ?? new MerchantConfigurationService(context);
    }
    
    [HttpPost("make-customization-without-register")]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("sensitive")]
    public async Task<IActionResult> MakeCustomizationWithoutRegister(GuestCustomizationRequest request)
    {
        var personalizationEnabled = await _configService.IsPersonalizationEnabledAsync();
        if (!personalizationEnabled)
        {
            if (request.Customizations.Any(c => !string.IsNullOrEmpty(c.DesignId) || !string.IsNullOrWhiteSpace(c.UserDescription)))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Personalization module is disabled for this store." });
            }
        }

        var guestUser = new GuestUser
        {
            Id = Guid.NewGuid(),
            FirstName = request.GuestData.FirstName,
            LastName = request.GuestData.LastName,
            Address = request.GuestData.Address,
            Country = request.GuestData.Country,
            Email = request.GuestData.Email,
            Zip = request.GuestData.Zip,
            PhoneNumber = request.GuestData.PhoneNumber,
            CreatedAt = LocalTimeService.LocalTime()
        };
        await _guestUsers.InsertOneAsync(guestUser);
        
        var designDictionary = new Dictionary<string, Design>();
        var productDictionary = new Dictionary<string, Product>();
        
        var customizations = new List<Customization>();
        var failedCustomizations = new List<string>();

        foreach (var req in request.Customizations)
        {
            Design? dbDesign = null;
            if (!string.IsNullOrEmpty(req.DesignId))
            {
                if (!Guid.TryParse(req.DesignId, out var designGuid))
                {
                    failedCustomizations.Add($"Neplatný DesignId: {req.DesignId}");
                    continue;
                }
                dbDesign = await _designs.Find(d => d.Id == designGuid).FirstOrDefaultAsync();
                if (dbDesign == null)
                {
                    failedCustomizations.Add($"Design not found: {req.DesignId}");
                    continue;
                }
            }

            if (!Guid.TryParse(req.ProductId, out var productGuid))
            {
                failedCustomizations.Add($"Neplatný ProductId: {req.ProductId}");
                continue;
            }
            var dbProduct = await _products.Find(p => p.Id == productGuid).FirstOrDefaultAsync();
            if (dbProduct == null)
            {
                failedCustomizations.Add($"Product not found: {req.ProductId}");
                continue;
            }

            var additionalPrice = !string.IsNullOrEmpty(req.UserDescription) ? 2.0M : 0.0M;
            var price = (dbDesign?.Price ?? 0.0M) + dbProduct.Price + additionalPrice;

            var newCustomization = new Customization
            {
                Id = Guid.NewGuid(),
                DesignId = req.DesignId,
                ProductId = req.ProductId,
                UserId = guestUser.Id.ToString(),
                UserDescription = req.UserDescription,
                Price = price,
                ProductColor = req.ProductColorName,
                ProductSize = req.ProductSize,
                CreatedAt = LocalTimeService.LocalTime()
            };

            customizations.Add(newCustomization);

            if (dbDesign != null && !string.IsNullOrEmpty(req.DesignId) && !designDictionary.ContainsKey(req.DesignId))
                designDictionary.Add(req.DesignId, dbDesign);

            if (!productDictionary.ContainsKey(req.ProductId))
                productDictionary.Add(req.ProductId, dbProduct);
        }

        if (customizations.Any())
        {
            await _customizations.InsertManyAsync(customizations);

            var cookieOptions = new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(7),
                HttpOnly = true,
                Secure = true
            };

            var customizationsJson = JsonSerializer.Serialize(customizations);
            Response.Cookies.Append("CartItems", customizationsJson, cookieOptions);
        }

        return Ok(new
        {
            GuestUserId = guestUser.Id,
            SuccessCustomization = customizations,
            Designs = designDictionary.Values,
            Products = productDictionary.Values,
            FailedRequests = failedCustomizations
        });
    }

    [HttpPost("make-order-without-register")]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("sensitive")]
    public async Task<IActionResult> MakeOrderWithoutRegister(
        GuestOrderRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyHeader = null)
    {
        if (request.CustomizationsId == null || request.CustomizationsId.Count == 0 ||
            !Guid.TryParse(request.GuestUserId, out var guestUserId))
            return BadRequest(new { error = "Invalid order request." });

        var guestUser = await _guestUsers.Find(g => g.Id == guestUserId).FirstOrDefaultAsync();
        if (guestUser == null) return BadRequest(new { error = "Invalid guest." });

        var effectiveIdempotencyKey = !string.IsNullOrWhiteSpace(request.IdempotencyKey)
            ? request.IdempotencyKey
            : idempotencyHeader;

        var result = await _orderService.CreateAsync(
            guestUserId,
            request.CustomizationsId,
            request.PaymentMethod,
            request.DeliveryMethod,
            request.PacketaPointId,
            request.PacketaPointName,
            request.PacketaPointAddress,
            effectiveIdempotencyKey);
        if (result.Error == OrderCreationError.InvalidItems)
            return BadRequest(new { error = "Invalid customizations." });
        if (result.Error == OrderCreationError.OutOfStock)
            return BadRequest(new { error = "Položka nie je na sklade." });
        if (result.Error == OrderCreationError.NumberConflict)
            return Conflict(new { error = "Could not allocate order number." });

        var order = result.Order!;
        return Ok(new
        {
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            CancellationToken = result.CancellationToken ?? order.CancellationToken,
            FollowToken = result.FollowToken ?? order.FollowToken
        });
    }
    
    [HttpPost("cancel-order-by-token")]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("public-write")]
    public async Task<IActionResult> CancelOrderByToken([FromBody] CapabilityTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            return NotFound(new { error = "Invalid or expired token!" });

        var result = await _orderLifecycleService.CancelOrderAsync(0, OrderActor.Guest(request.Token), request.Token);
        if (!result.IsSuccess)
            return NotFound(new { error = "Invalid or expired token!" });

        return Ok(new { message = "Order canceled successfully!" });
    }
    
    [HttpPost("cancel")]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("public-write")]
    public async Task<IActionResult> CancelOrder([FromBody] CapabilityTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            return BadRequest("Cancellation is required!");

        var result = await _orderLifecycleService.CancelOrderAsync(0, OrderActor.Guest(request.Token), request.Token);
        if (!result.IsSuccess)
            return NotFound("Order not found.");

        return Ok(new { message = "Objednávka bola úspešne zrušená." });
    }
    
    [HttpPost("follow-order")]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("public-read")]
    public async Task<IActionResult> ConfirmPayment([FromBody] CapabilityTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token)) return BadRequest();
        var tokenHash = CapabilityToken.Hash(request.Token);
        var order = await _orderStore.GetByFollowTokenHashAsync(tokenHash);
        if (order == null || (order.FollowTokenExpiresAt.HasValue && order.FollowTokenExpiresAt.Value <= DateTime.UtcNow))
            return NotFound(new { error = "Order not found" });

        var guestUser = await _guestUsers.Find(g => g.Id == order.UserId).FirstOrDefaultAsync();

        if (OrderSnapshotPresentation.HasSnapshots(order))
        {
            var (snapshotCustomizations, snapshotProducts, snapshotDesigns) = OrderSnapshotPresentation.MaterializeDetails(order);
            return Ok(new 
            {
                order = new { order.Id, order.OrderNumber, order.TotalPrice, order.StatusOrder, order.CreatedAt },
                customizations = snapshotCustomizations,
                designs = snapshotDesigns,
                products = snapshotProducts,
                user = guestUser != null ? new
                {
                    FirstName = (string?)guestUser.FirstName,
                    LastName = (string?)guestUser.LastName,
                    Email = (string?)guestUser.Email,
                    PhoneNumber = guestUser.PhoneNumber,
                    Address = guestUser.Address,
                    Country = guestUser.Country,
                    Zip = guestUser.Zip
                } : (order.CustomerSnapshot != null ? new
                {
                    FirstName = (string?)order.CustomerSnapshot.FirstName,
                    LastName = (string?)order.CustomerSnapshot.LastName,
                    Email = (string?)order.CustomerSnapshot.Email,
                    PhoneNumber = order.CustomerSnapshot.PhoneNumber,
                    Address = order.CustomerSnapshot.Address,
                    Country = order.CustomerSnapshot.Country,
                    Zip = order.CustomerSnapshot.Zip
                } : null)
            });
        }
        
        var customizationIds = order.Customizations;
        var customizations = await _customizations.Find(c => customizationIds.Contains(c.Id)).ToListAsync();

        var productIds = customizations
            .Select(c => c.ProductId)
            .Distinct()
            .ToList();
        
        var products = await _products
            .Find(p => productIds.Contains(p.Id.ToString()))
            .ToListAsync();

        var customProductColors = customizations
            .Select(c => new { ProductId = c.ProductId, Colors = c.ProductColor })
            .Distinct()
            .ToList();
        
        var filteredProducts = products.Select(p =>
        {
            var requestedColors = customProductColors
                .Where(x => x.ProductId == p.Id.ToString())
                .Select(x => x.Colors)
                .Distinct()
                .ToList();
            p.Colors = p.Colors.Where(color => requestedColors.Contains(color.Name)).ToList();
            return p;
        }).ToList();
        
        var designIds = customizations
            .Select(c => c.DesignId)
            .Distinct()
            .ToList();
        var designGuids = designIds.Select(id => Guid.Parse(id)).ToList();
        var designs = await _designs.Find(d => designGuids.Contains(d.Id)).ToListAsync();

        return Ok(new 
        {
            order = new { order.Id, order.OrderNumber, order.TotalPrice, order.StatusOrder, order.CreatedAt },
            customizations,
            designs,
            products = filteredProducts,
            user = guestUser == null ? null : new
            {
                guestUser.FirstName,
                guestUser.LastName,
                guestUser.Email,
                guestUser.PhoneNumber,
                guestUser.Address,
                guestUser.Country,
                guestUser.Zip
            }
        });
    }

    [HttpPost("order/{OrderId}")]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("public-read")]
    public async Task<IActionResult> OrderInformationById(int OrderId, [FromBody] CapabilityTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token)) return BadRequest();
        var tokenHash = CapabilityToken.Hash(request.Token);
        var dbOrder = await _orderStore.GetByIdAsync(OrderId);
        if (dbOrder == null || dbOrder.FollowToken != tokenHash || (dbOrder.FollowTokenExpiresAt.HasValue && dbOrder.FollowTokenExpiresAt.Value <= DateTime.UtcNow))
            return NotFound();

        return Ok(new { dbOrder.Id, dbOrder.OrderNumber, dbOrder.TotalPrice, dbOrder.StatusOrder, dbOrder.CreatedAt });
    }
}

public sealed class CapabilityTokenRequest
{
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.StringLength(128, MinimumLength = 32)]
    public string Token { get; set; } = string.Empty;
}
