using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using nia_api.Data;
using nia_api.Enums;
using nia_api.Models;
using nia_api.Requests;
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
    private readonly IMongoCollection<Order> _orders;
    private readonly OrderService _orderService;

    public GuestUserController(NiaDbContext context, OrderService orderService)
    {
        _products = context.Products;
        _guestUsers = context.GuestUsers;
        _designs = context.Designs;
        _customizations = context.Customizations;
        _orders = context.Orders;
        _orderService = orderService;
    }
    
    [HttpPost("make-customization-without-register")]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("sensitive")]
    public async Task<IActionResult> MakeCustomizationWithoutRegister(GuestCustomizationRequest request)
    {
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
            if (!Guid.TryParse(req.DesignId, out var designGuid))
            {
                failedCustomizations.Add($"Neplatný DesignId: {req.DesignId}");
                continue;
            }
            var dbDesign = await _designs.Find(d => d.Id == designGuid).FirstOrDefaultAsync();
            if (dbDesign == null)
            {
                failedCustomizations.Add($"Design not found: {req.DesignId}");
                continue;
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
            var price = dbDesign.Price + dbProduct.Price + additionalPrice;

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

            if (!designDictionary.ContainsKey(req.DesignId))
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
    public async Task<IActionResult> MakeOrderWithoutRegister(GuestOrderRequest request)
    {
        if (request.CustomizationsId == null || request.CustomizationsId.Count == 0 ||
            !Guid.TryParse(request.GuestUserId, out var guestUserId))
            return BadRequest(new { error = "Invalid order request." });

        var guestUser = await _guestUsers.Find(g => g.Id == guestUserId).FirstOrDefaultAsync();
        if (guestUser == null) return BadRequest(new { error = "Invalid guest." });

        var result = await _orderService.CreateAsync(
            guestUserId,
            request.CustomizationsId,
            request.PaymentMethod,
            request.DeliveryMethod,
            request.PacketaPointId,
            request.PacketaPointName,
            request.PacketaPointAddress);
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
            CancellationToken = order.CancellationToken,
            FollowToken = order.FollowToken
        });
    }
    
    [NonAction]
    public async Task<IActionResult> CancelOrder(int OrderId)
    {
        var dbOrder = _orders.Find(o => o.Id == OrderId).FirstOrDefault();

        if (dbOrder == null)
            return NotFound(new { error = "Order not founded!" });
        
        var filterOrder = Builders<Order>.Filter.Eq(o => o.Id, OrderId);
        var updateDefinition = Builders<Order>.Update.Set(o => o.StatusOrder, EStatus.ZRUSENA);
        var resultOrder = await _orders.UpdateOneAsync(filterOrder, updateDefinition);

        if (resultOrder.MatchedCount == 0)
            return NotFound(new { error = "Order not found!" });

        return Ok(new { message = "Order is updated!" });
    }
    
    [HttpPost("cancel-order-by-token")]
    public async Task<IActionResult> CancelOrderByToken(string token)
    {
        var success = await _orderService.CancelByTokenAsync(token);
        if (!success)
            return NotFound(new { error = "Invalid or expired token!" });

        return Ok(new { message = "Order canceled successfully!" });
    }
    
    [NonAction]
    public async Task<IActionResult> DecrementProductQuantity(CustomizationRequest request)
    {
        Guid.TryParse(request.ProductId, out var _productId);
        var dbProduct = await _products.Find(p => p.Id == _productId).FirstOrDefaultAsync();
        
        if (dbProduct == null)
            return NotFound(new { error = "Product not found" });
        
        var filter = Builders<Product>.Filter.And(
            Builders<Product>.Filter.Eq(p => p.Id, _productId),
            Builders<Product>.Filter.ElemMatch(p => p.Colors, c => c.Name == request.ProductColorName && c.Sizes.Any(s => s.Size == request.ProductSize && s.Quantity > 0))
        );
        
        var update = Builders<Product>.Update.Inc("colors.$[color].sizes.$[size].quantity", -1);

        var arrayFilters = new[]
        {
            new BsonDocumentArrayFilterDefinition<BsonDocument>(new BsonDocument("color.name", request.ProductColorName)),
            new BsonDocumentArrayFilterDefinition<BsonDocument>(new BsonDocument("size.size", request.ProductSize))
        };

        var updateOptions = new UpdateOptions { ArrayFilters = arrayFilters };
        var updateResult = await _products.UpdateOneAsync(filter, update, updateOptions);

        if (updateResult.MatchedCount == 0)
        {
            return NotFound(new { error = "Color or size not found, or quantity is already zero." });
        }

        if (updateResult.ModifiedCount == 0)
        {
            return BadRequest(new { error = "Failed to decrement quantity." });
        }
        
        return Ok(new { message = "Product quantity decremented successfully!" });
    }
    
    [HttpPost("cancel")]
    public async Task<IActionResult> CancelOrder([FromQuery] string cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(cancellationToken))
            return BadRequest("Cancellation is required!");

        var success = await _orderService.CancelByTokenAsync(cancellationToken);
        if (!success)
            return NotFound("Order not found.");

        return Ok(new { message = "Objednávka bola úspešne zrušená." });
    }
    
    [NonAction]
    public async Task<IActionResult> ConfirmPayment([FromQuery] int orderId)
    {
        var filter = Builders<Order>.Filter.Eq(o => o.Id, orderId);
        var update = Builders<Order>.Update.Set(o => o.StatusOrder, EStatus.ZAPLATENA);
        
        var updateResult = await _orders.UpdateOneAsync(filter, update);
        
        if (updateResult.MatchedCount == 0)
            return NotFound("Order not found.");
        
        return Ok(new { message = "Objednávka bola úspešne zaplatená." });
    }
    
    [HttpPost("follow-order")]
    public async Task<IActionResult> ConfirmPayment([FromQuery] string followToken)
    {
        var order = await _orders.Find(o => o.FollowToken == followToken).FirstOrDefaultAsync();
        if (order == null)
            return NotFound(new { error = "Order not found" });
        
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

        var guestUser = await _guestUsers.Find(g => g.Id == order.UserId).FirstOrDefaultAsync();

        return Ok(new 
        {
            order = new { order.Id, order.TotalPrice, order.StatusOrder, order.CreatedAt },
            customizations,
            designs,
            products = filteredProducts,
            user = guestUser
        });
    }

    [HttpGet("order/{OrderId}")]
    public async Task<IActionResult> OrderInformationById(int OrderId, [FromQuery] string followToken)
    {
        if (string.IsNullOrWhiteSpace(followToken)) return BadRequest();
        var dbOrder = await _orders.Find(o => o.Id == OrderId && o.FollowToken == followToken).FirstOrDefaultAsync();

        return dbOrder == null ? NotFound() : Ok(new { dbOrder.Id, dbOrder.TotalPrice, dbOrder.StatusOrder, dbOrder.CreatedAt, dbOrder.FollowToken });
    }
}
