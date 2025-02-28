using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using nia_api.Data;
using nia_api.Enums;
using nia_api.Models;
using nia_api.Requests;
using nia_api.Services;

namespace nia_api.Controllers;

//TODO: Keď vytvorím objednávku poslať email

[ApiController]
[Route("guest")]
public class GuestUserController : ControllerBase
{
    private readonly IMongoCollection<Product> _products;
    private readonly IMongoCollection<GuestUser> _guestUsers;
    private readonly IMongoCollection<Design> _designs;
    private readonly IMongoCollection<Customization> _customizations;
    private readonly IMongoCollection<Order> _orders;

    public GuestUserController(NiaDbContext context)
    {
        _products = context.Products;
        _guestUsers = context.GuestUsers;
        _designs = context.Designs;
        _customizations = context.Customizations;
        _orders = context.Orders;
    }
    
    [HttpPost("make-customization-without-register")]
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
        
        var designIds = request.Customizations.Select(c => Guid.Parse(c.DesignId)).ToList();
        var dbDesigns = await _designs.Find(d => designIds.Contains(d.Id)).ToListAsync();

        var productIds = request.Customizations.Select(c => Guid.Parse(c.ProductId)).ToList();
        var dbProducts = await _products.Find(p => productIds.Contains(p.Id)).ToListAsync();
        
        var customizations = new List<Customization>();
        var failedCustomizations = new List<string>();
        
        foreach (var customization in request.Customizations)
        {
            var dbDesign = dbDesigns.FirstOrDefault(d => d.Id.ToString() == customization.DesignId);
            if (dbDesign == null)
            {
                failedCustomizations.Add($"Design not found: {customization.DesignId}");
                continue;
            }

            var dbProduct = dbProducts.FirstOrDefault(p => p.Id.ToString() == customization.ProductId);
            if (dbProduct == null)
            {
                failedCustomizations.Add($"Product not found: {customization.ProductId}");
                continue;
            }

            var price = dbDesign.Price + dbProduct.Price;
            if (!string.IsNullOrEmpty(customization.UserDescription))
                price += 2.0M;

            var newCustomization = new Customization
            {
                Id = Guid.NewGuid(),
                DesignId = customization.DesignId,
                ProductId = customization.ProductId,
                UserId = guestUser.Id.ToString(),
                UserDescription = customization.UserDescription,
                Price = price,
                CreatedAt = LocalTimeService.LocalTime(),
            };

            customizations.Add(newCustomization);
        }
        
        if (customizations.Any())
            await _customizations.InsertManyAsync(customizations);

        return Ok(new
        {
            GuestUserId = guestUser.Id,
            SuccessIds = customizations.Select(c => c.Id),
            FailedRequests = failedCustomizations
        });
    }

    [HttpPost("make-order-without-register")]
    public async Task<IActionResult> MakeOrderWithoutRegister(GuestOrderRequest request)
    {
        var dbCustomization = await _customizations.Find(c => request.CustomizationsId.Contains(c.Id)).ToListAsync();

        var totalPrice = dbCustomization.Sum(c => c.Price);

        var lastOrder = await _orders.Find(FilterDefinition<Order>.Empty)
            .SortByDescending(o => o.Id) 
            .FirstOrDefaultAsync();

        int newIntId = lastOrder != null ? lastOrder.Id + 1 : 1;
        
        var cancellationToken = Guid.NewGuid().ToString();

        var lcOrder = new Order
        {
            Id = newIntId,
            Customizations = request.CustomizationsId,
            TotalPrice = totalPrice,
            UserId = Guid.Parse(request.GuestUserId),
            StatusOrder = EStatus.PRIJATA,
            CancellationToken = cancellationToken,
            CreatedAt = LocalTimeService.LocalTime()
        };

        await _orders.InsertOneAsync(lcOrder);

        return Ok(new { message = "Order is successful!" });
    }
    
    [HttpPost("cancel-order/{OrderId}")]
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
        var dbOrder = await _orders.Find(o => o.CancellationToken == token).FirstOrDefaultAsync();

        if (dbOrder == null)
            return NotFound(new { error = "Invalid or expired token!" });

        var filterOrder = Builders<Order>.Filter.Eq(o => o.CancellationToken, token);
        var updateDefinition = Builders<Order>.Update.Set(o => o.StatusOrder, EStatus.ZRUSENA);
        var resultOrder = await _orders.UpdateOneAsync(filterOrder, updateDefinition);

        if (resultOrder.MatchedCount == 0)
            return NotFound(new { error = "Order not found!" });

        return Ok(new { message = "Order canceled successfully!" });
    }
    
    [HttpDelete("decrement-product-quantity")]
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
}