using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using nia_api.Data;
using nia_api.Enums;
using nia_api.Models;
using nia_api.Requests;
using nia_api.Services;

namespace nia_api.Controllers;

/*
 * TODO:
 * Platobnú branu
 */

[ApiController]
[Authorize]
[Route("user")]
public class UserController : ControllerBase
{
    private readonly IMongoCollection<User> _users;
    private readonly IMongoCollection<Customization> _customizations;
    private readonly IMongoCollection<Design> _designs;
    private readonly IMongoCollection<Product> _products;
    private readonly IMongoCollection<Order> _orders;

    private readonly HeaderReaderService _headerReader;

    public UserController(NiaDbContext context, HeaderReaderService headerReader)
    {
        _users = context.Users;
        _headerReader = headerReader;
        _customizations = context.Customizations;
        _designs = context.Designs;
        _products = context.Products;
        _orders = context.Orders;
    }
    
    [HttpGet("profile")]
    public async Task<IActionResult> GetUserProfile()
    {
        var userId = await _headerReader.GetUserIdAsync(User);

        if (userId == null)
            return Unauthorized(new { error = "User ID not found in token!" });

        var dbUser = await _users.Find(u => u.Id == userId.Value).FirstOrDefaultAsync();
        if (dbUser == null)
            return NotFound(new { error = "User not found!" });

        return Ok(dbUser);
    }
    
    [HttpGet("my-customizations")]
    public async Task<IActionResult> GetMyCustomizations()
    {
        var userId = await _headerReader.GetUserIdAsync(User);

        if (userId == null)
            return Unauthorized(new { error = "User ID not found in token!" });

        var dbUserCustomization = _customizations.Find(c => c.UserId == userId.Value.ToString()).ToListAsync();

        if (dbUserCustomization == null)
            return NotFound(new { error = "Customization for this user not founded!" });

        return Ok(dbUserCustomization);
    }

    [HttpPost("make-order")]
    public async Task<IActionResult> MakeOrder(List<string> customizationsIds)
    {
        var userId = await _headerReader.GetUserIdAsync(User);

        if (userId == null)
            return Unauthorized(new { error = "User ID not found in token!" });

        var dbCustomization = await _customizations.Find(c => customizationsIds.Contains(c.Id.ToString())).ToListAsync();

        var totalPrice = dbCustomization.Sum(c => c.Price);

        var lastOrder = await _orders.Find(FilterDefinition<Order>.Empty)
            .SortByDescending(o => o.Id) 
            .FirstOrDefaultAsync();

        int newIntId = lastOrder != null ? lastOrder.Id + 1 : 1;

        var lcOrder = new Order
        {
            Id = newIntId,
            Customizations = customizationsIds,
            TotalPrice = totalPrice,
            UserId = userId.Value,
            StatusOrder = EStatus.PRIJATA,
            CreatedAt = LocalTimeService.LocalTime()
        };

        await _orders.InsertOneAsync(lcOrder);

        return Ok(new { message = "Order successfully created!" });
    }

    [HttpPost("cancel-order/{OrderId}")]
    public async Task<IActionResult> CancelOrder(int OrderId)
    {
        var userId = await _headerReader.GetUserIdAsync(User);

        if (userId == null)
            return Unauthorized(new { error = "User ID not found in token!" });

        var dbOrder = _orders.Find(o => o.Id == OrderId).FirstOrDefault();

        if (dbOrder == null)
            return NotFound(new { error = "Order not founded!" });
        
        var filterOrder = Builders<Order>.Filter.Eq(o => o.UserId, userId.Value);
        var updateDefinition = Builders<Order>.Update.Set(o => o.StatusOrder, EStatus.ZRUSENA);
        var resultOrder = await _orders.UpdateOneAsync(filterOrder, updateDefinition);

        if (resultOrder.MatchedCount == 0)
            return NotFound(new { error = "Order not found!" });

        return Ok(new { message = "Order is updated!" });
    }

    [HttpPut("update")]
    public async Task<IActionResult> UpdateUserProfile(User user)
    {
        var userId = await _headerReader.GetUserIdAsync(User);

        if (userId == null)
            return Unauthorized(new { error = "User ID not found in token!" });

        var filterUser = Builders<User>.Filter.Eq(u => u.Id, userId.Value);
        var resultUser = await _users.ReplaceOneAsync(filterUser, user);
        
        if (resultUser.MatchedCount == 0)
            return NotFound(new { error = "User not found!" });
        
        return Ok(new { message = "User is updated!"});
    }

    [HttpDelete("remove")]
    public async Task<IActionResult> RemoveUser()
    {
        var userId = await _headerReader.GetUserIdAsync(User);

        if (userId == null)
            return Unauthorized(new { error = "User ID not found in token!" });

        await _users.DeleteOneAsync(u => u.Id == userId.Value);
        
        return Ok(new { message = "User successful removed!" });
    }
    
    [HttpPost("make-customization")]
    public async Task<IActionResult> Custom(CustomizationRequest request)
    {
        var userId = await _headerReader.GetUserIdAsync(User);

        if (userId == null)
            return Unauthorized(new { error = "User ID not found in token!" });

        Guid.TryParse(request.DesignId, out var designId);
        var dbDesign = await _designs.Find(d => d.Id == designId).FirstOrDefaultAsync();

        if (dbDesign == null)
            return NotFound(new { error = "Design not found." });

        Guid.TryParse(request.ProductId, out var productId);
        var dbProduct = await _products.Find(p => p.Id == productId).FirstOrDefaultAsync();

        if (dbProduct == null)
            return NotFound(new { error = "Product not found" });

        var price = 0.0M;

        if (request.UserDescription != null)
            price = 2.0M;

        var newCustomization = new Customization()
        {
            Id = Guid.NewGuid(),
            DesignId = request.DesignId,
            ProductId = request.ProductId,
            UserId = userId.Value.ToString(),
            UserDescription = request.UserDescription,
            Price = price + dbDesign.Price + dbProduct.Price,
            CreatedAt = LocalTimeService.LocalTime()
        };

        await _customizations.InsertOneAsync(newCustomization);
        
        return Ok(new { message = "Customization successful created!" });
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