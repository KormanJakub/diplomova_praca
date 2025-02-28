using Amazon.Runtime.Internal.Util;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using nia_api.Data;
using nia_api.Enums;
using nia_api.Models;
using nia_api.Requests;
using nia_api.Services;
using Stripe.Checkout;

namespace nia_api.Controllers;

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

    [HttpGet("orders")]
    public async Task<IActionResult> GetOrders()
    {
        var userId = await _headerReader.GetUserIdAsync(User);
        if (userId == null)
            return Unauthorized(new { error = "User ID not found in token!" });

        var orders = await _orders.Find(o => o.UserId == userId).ToListAsync();

        var customizations = await _customizations.Find(c => c.UserId == userId.ToString()).ToListAsync();

        var designIds = customizations.Select(c => c.DesignId).Distinct().ToList();
        var designs = await _designs.Find(d => designIds.Contains(d.Id.ToString())).ToListAsync();

        var productColorMap = customizations
            .GroupBy(c => c.ProductId)
            .ToDictionary(
                g => g.Key, 
                g => g.Select(c => c.ProductColor).Distinct().ToList()
            );

        var productIds = productColorMap.Keys.ToList();
        var products = await _products.Find(p => productIds.Contains(p.Id.ToString())).ToListAsync();

        var filteredProducts = products.Select(p =>
        {
            var prodId = p.Id.ToString();
            if (productColorMap.TryGetValue(prodId, out var requiredColors))
            {
                p.Colors = p.Colors.Where(color => requiredColors.Contains(color.Name)).ToList();
            }
            return p;
        }).ToList();

        return Ok(new 
        {
            orders,
            customizations,
            designs,
            products = filteredProducts
        });
    }
    
    [HttpGet("orders/{Id}")]
    public async Task<IActionResult> GetOrdersById(int Id)
    {
        var userId = await _headerReader.GetUserIdAsync(User);
        if (userId == null)
            return Unauthorized(new { error = "User ID not found in token!" });

        var dbUser = await _users.Find(u => u.Id == userId).FirstOrDefaultAsync();

        var order = await _orders.Find(o => o.Id == Id).FirstOrDefaultAsync();
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

        return Ok(new 
        {
            order,
            customizations,
            designs,
            products = filteredProducts,
            user = dbUser
        });
    }


    [HttpPost("make-order")]
    public async Task<IActionResult> MakeOrder(List<Guid> customizationsIds)
    {
        var userId = await _headerReader.GetUserIdAsync(User);

        if (userId == null)
            return Unauthorized(new { error = "User ID not found in token!" });

        var dbCustomization = await _customizations.Find(c => customizationsIds.Contains(c.Id)).ToListAsync();

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

        return Ok(new { message = "Order is successful!" });
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
    public async Task<IActionResult> Custom(List<CustomizationRequest> requests)
    {
        var userId = await _headerReader.GetUserIdAsync(User);

        if (userId == null)
            return Unauthorized(new { error = "User ID not found in token!" });

        var customizations = new List<Customization>();
        var failedCustomizations = new List<string>();

        foreach (var request in requests)
        {
            Guid.TryParse(request.DesignId, out var designId);
            var dbDesign = await _designs.Find(d => d.Id == designId).FirstOrDefaultAsync();

            if (dbDesign == null)
            {
                failedCustomizations.Add($"Design not found: {request.DesignId}");
                continue;
            }

            Guid.TryParse(request.ProductId, out var productId);
            var dbProduct = await _products.Find(p => p.Id == productId).FirstOrDefaultAsync();

            if (dbProduct == null)
            {
                failedCustomizations.Add($"Product not found: {request.ProductId}");
                continue;
            }

            var price = 0.0M;

            if (!string.IsNullOrEmpty(request.UserDescription))
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

            customizations.Add(newCustomization);
        }

        if (customizations.Any())
            await _customizations.InsertManyAsync(customizations);

        return Ok(new
        {
            SuccessIds = customizations.Select(c => c.Id),
            FailedRequests = failedCustomizations        
        });
    }
}