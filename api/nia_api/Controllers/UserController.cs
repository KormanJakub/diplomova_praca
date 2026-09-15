using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using nia_api.Data;
using nia_api.Domain.Configuration;
using nia_api.Domain.Orders;
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
    private readonly IOrderStore _orderStore;
    private readonly IOrderLifecycleService _orderLifecycleService;

    private readonly HeaderReaderService _headerReader;
    private readonly OrderService _orderService;
    private readonly IMerchantConfigurationService _configService;

    public UserController(
        NiaDbContext context,
        HeaderReaderService headerReader,
        OrderService orderService,
        IOrderStore orderStore,
        IOrderLifecycleService orderLifecycleService,
        IMerchantConfigurationService? configService = null)
    {
        _users = context.Users;
        _headerReader = headerReader;
        _orderService = orderService;
        _customizations = context.Customizations;
        _designs = context.Designs;
        _products = context.Products;
        _orderStore = orderStore;
        _orderLifecycleService = orderLifecycleService;
        _configService = configService ?? new MerchantConfigurationService(context);
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

        return Ok(UserResponse.From(dbUser));
    }
    
    [HttpGet("my-customizations")]
    public async Task<IActionResult> GetMyCustomizations()
    {
        var userId = await _headerReader.GetUserIdAsync(User);

        if (userId == null)
            return Unauthorized(new { error = "User ID not found in token!" });

        var dbUserCustomization = await _customizations.Find(c => c.UserId == userId.Value.ToString()).ToListAsync();

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

        var orders = await _orderStore.GetByUserIdAsync(userId.Value);

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

        var order = await _orderStore.GetByIdAsync(Id);
        if (order == null || order.UserId != userId.Value)
            return NotFound(new { error = "Order not found" });

        if (OrderSnapshotPresentation.HasSnapshots(order))
        {
            var (snapshotCustomizations, snapshotProducts, snapshotDesigns) = OrderSnapshotPresentation.MaterializeDetails(order);
            return Ok(new 
            {
                order,
                customizations = snapshotCustomizations,
                designs = snapshotDesigns,
                products = snapshotProducts,
                user = dbUser != null ? UserResponse.From(dbUser) : (order.CustomerSnapshot != null ? new UserResponse(
                    order.CustomerSnapshot.UserId,
                    order.CustomerSnapshot.Email,
                    true,
                    order.CustomerSnapshot.FirstName,
                    order.CustomerSnapshot.LastName,
                    order.CustomerSnapshot.Country,
                    order.CustomerSnapshot.PhoneNumber,
                    order.CustomerSnapshot.Address,
                    order.CustomerSnapshot.Zip,
                    false) : null)
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
            order,
            customizations,
            designs,
            products = filteredProducts,
            user = dbUser == null ? null : UserResponse.From(dbUser)
        });
    }


    [HttpPost("make-order")]
    public async Task<IActionResult> MakeOrder(
        List<Guid> customizationsIds,
        [FromQuery] string? paymentMethod = "Stripe",
        [FromQuery] string? deliveryMethod = "HomeDelivery",
        [FromQuery] string? packetaPointId = null,
        [FromQuery] string? packetaPointName = null,
        [FromQuery] string? packetaPointAddress = null,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey = null)
    {
        var userId = await _headerReader.GetUserIdAsync(User);

        if (userId == null)
            return Unauthorized(new { error = "User ID not found in token!" });

        var result = await _orderService.CreateAsync(
            userId.Value,
            customizationsIds,
            paymentMethod,
            deliveryMethod,
            packetaPointId,
            packetaPointName,
            packetaPointAddress,
            idempotencyKey);
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
            order.TotalPrice,
            CancellationToken = result.CancellationToken ?? order.CancellationToken,
            FollowToken = result.FollowToken ?? order.FollowToken,
            OrderNumber = order.OrderNumber
        });
    }

    [HttpPost("cancel-order/{OrderId}")]
    public async Task<IActionResult> CancelOrder(int OrderId)
    {
        var userId = await _headerReader.GetUserIdAsync(User);

        if (userId == null)
            return Unauthorized(new { error = "User ID not found in token!" });

        var result = await _orderLifecycleService.CancelOrderAsync(OrderId, OrderActor.Customer(userId.Value));
        if (!result.IsSuccess)
            return NotFound(new { error = "Order not found!" });

        return Ok(new { message = "Order is updated!" });
    }

    [HttpPut("update")]
    public async Task<IActionResult> UpdateUserProfile(nia_api.Requests.UpdateProfileRequest user)
    {
        var userId = await _headerReader.GetUserIdAsync(User);

        if (userId == null)
            return Unauthorized(new { error = "User ID not found in token!" });

        var filterUser = Builders<User>.Filter.Eq(u => u.Id, userId.Value);
        var update = Builders<User>.Update
            .Set(u => u.FirstName, user.FirstName)
            .Set(u => u.LastName, user.LastName)
            .Set(u => u.Country, user.Country)
            .Set(u => u.PhoneNumber, user.PhoneNumber)
            .Set(u => u.Address, user.Address)
            .Set(u => u.Zip, user.Zip)
            .Set(u => u.UpdatedAt, DateTime.UtcNow);
        var resultUser = await _users.UpdateOneAsync(filterUser, update);
        
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
        var personalizationEnabled = await _configService.IsPersonalizationEnabledAsync();
        if (!personalizationEnabled)
        {
            if (requests.Any(c => !string.IsNullOrEmpty(c.DesignId) || !string.IsNullOrWhiteSpace(c.UserDescription)))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Personalization module is disabled for this store." });
            }
        }

        var userId = await _headerReader.GetUserIdAsync(User);
        var designDictionary = new Dictionary<string, Design>();
        var productDictionary = new Dictionary<string, Product>();

        if (userId == null)
            return Unauthorized(new { error = "User ID not found in token!" });

        var customizations = new List<Customization>();
        var failedCustomizations = new List<string>();

        foreach (var request in requests)
        {
            Design? dbDesign = null;
            if (!string.IsNullOrEmpty(request.DesignId))
            {
                Guid.TryParse(request.DesignId, out var designId);
                dbDesign = await _designs.Find(d => d.Id == designId).FirstOrDefaultAsync();

                if (dbDesign == null)
                {
                    failedCustomizations.Add($"Design not found: {request.DesignId}");
                    continue;
                }
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
                ProductColor = request.ProductColorName,
                ProductSize = request.ProductSize,
                UserId = userId.Value.ToString(),
                UserDescription = request.UserDescription,
                Price = price + (dbDesign?.Price ?? 0.0M) + dbProduct.Price,
                CreatedAt = LocalTimeService.LocalTime()
            };

            customizations.Add(newCustomization);
            
            if (dbDesign != null && !string.IsNullOrEmpty(request.DesignId) && !designDictionary.ContainsKey(request.DesignId))
                designDictionary.Add(request.DesignId, dbDesign);

            if (!productDictionary.ContainsKey(request.ProductId))
                productDictionary.Add(request.ProductId, dbProduct);
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
            SuccessCustomization = customizations,
            Designs = designDictionary.Values,
            Products = productDictionary.Values,
            FailedRequests = failedCustomizations      
        });
    }
}
