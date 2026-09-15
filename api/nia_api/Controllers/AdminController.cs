using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using nia_api.Data;
using nia_api.Domain.Configuration;
using nia_api.Domain.Orders;
using nia_api.Enums;
using nia_api.Models;
using nia_api.Requests;
using nia_api.Services;
using Tag = nia_api.Models.Tag;

namespace nia_api.Controllers;

[ApiController]
[Route("admin")]
[Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")]
public class AdminController : ControllerBase
{
    private readonly IMongoCollection<Design> _designs;
    private readonly IMongoCollection<Product> _products;
    private readonly IMongoCollection<Tag> _tags;
    private readonly IMongoCollection<PairedDesign> _pairedDesigns;
    private readonly IMongoCollection<Customization> _customizations;
    private readonly IOrderStore _orderStore;
    private readonly IOrderLifecycleService _orderLifecycleService;
    private readonly IMongoCollection<User> _users;
    private readonly IMongoCollection<GuestUser> _guestUsers;
    private readonly IMongoCollection<StoreSettings> _storeSettings;
    private readonly OrderService _orderService;
    private readonly IMerchantConfigurationService _configService;
    
    public AdminController(
        NiaDbContext context,
        OrderService orderService,
        IOrderStore orderStore,
        IOrderLifecycleService orderLifecycleService,
        IMerchantConfigurationService? configService = null)
    {
        _designs = context.Designs;
        _products = context.Products;
        _tags = context.Tags;
        _pairedDesigns = context.PairedDesigns;
        _customizations = context.Customizations;
        _orderStore = orderStore;
        _orderLifecycleService = orderLifecycleService;
        _users = context.Users;
        _guestUsers = context.GuestUsers;
        _storeSettings = context.StoreSettings;
        _orderService = orderService;
        _configService = configService ?? new MerchantConfigurationService(context);
    }

    [HttpGet("tag/getAll")]
    public async Task<IActionResult> GetAllTags()
    {
        var dbTags = await _tags.Find(_ => true).ToListAsync();

        if (dbTags == null || dbTags.Count == 0)
            return NotFound(new { error = "No tags found!" });
        
        return Ok(dbTags);
    }

    [HttpGet("product/by/{tagId}")]
    public async Task<IActionResult> GetForSpecificTagAllProducts(string tagId)
    {
        var dbProducts = await _products.Find(p => p.TagId == Guid.Parse(tagId)).ToListAsync();

        if (dbProducts == null || dbProducts.Count == 0)
            return NotFound(new { error = "No products by Tag founded!" });

        return Ok(dbProducts);
    }
    
    [HttpGet("product/getAll")]
    public async Task<IActionResult> GetAllProducts()
    {
        var dbProducts = await _products.Find(_ => true).ToListAsync();

        if (dbProducts == null || dbProducts.Count == 0)
            return NotFound(new { error = "No products found!" });
        
        return Ok(dbProducts);
    }

    [HttpGet("design/getAll")]
    public async Task<IActionResult> GetAllDesigns()
    {
        if (!await _configService.IsPersonalizationEnabledAsync())
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Personalization module is disabled for this store." });

        var dbDesigns = await _designs.Find(_ => true).ToListAsync();

        if (dbDesigns == null || dbDesigns.Count == 0)
            return NotFound(new { error = "No designs found!"});

        return Ok(dbDesigns);
    }

    [HttpGet("design/all-paired-designs")]
    public async Task<IActionResult> GetAllPairedDesgings()
    {
        if (!await _configService.IsPersonalizationEnabledAsync())
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Personalization module is disabled for this store." });

        var dbPairedDesigns = await _pairedDesigns.Find(_ => true).ToListAsync();

        if (dbPairedDesigns.Count == 0 || dbPairedDesigns == null)
            return NotFound(new { error = "No paired designs!" });

        var designIds = dbPairedDesigns
            .SelectMany(pd => pd.DesignIds)
            .Distinct()
            .ToList();

        var designs = await _designs.Find(d => designIds.Contains(d.Id)).ToListAsync();

        return Ok(new
        {
            PairedDesign = dbPairedDesigns,
            Design = designs
        });
    }

    [HttpGet("design/paired-design/designs-in-pair/{pairedDesignId}")]
    public async Task<IActionResult> GetDesignsInPair(string pairedDesignId)
    {
        if (!await _configService.IsPersonalizationEnabledAsync())
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Personalization module is disabled for this store." });

        Guid parsedPairedDesignId;
        if (!Guid.TryParse(pairedDesignId, out parsedPairedDesignId))
            return BadRequest(new { error = "Invalid pairedDesignId format." });

        var dbPairedDesigns = await _pairedDesigns
            .Find(pd => pd.Id == parsedPairedDesignId)
            .FirstOrDefaultAsync();

        if (dbPairedDesigns == null)
            return NotFound(new { error = "Paired design not found." });
        
        if (dbPairedDesigns.DesignIds == null || !dbPairedDesigns.DesignIds.Any())
            return NotFound(new { error = "No designs found for this paired design." });
        
        var dbDesigns = await _designs
            .Find(d => dbPairedDesigns.DesignIds.Contains(d.Id))
            .ToListAsync();

        return Ok(dbDesigns);
    }

    [HttpPost("tag/create")]
    public async Task<IActionResult> CreateTag(Tag tag)
    {
        if (tag == null)
            return BadRequest(new { error = "Tag is empty!" });

        var dbTag = await _tags.Find(t => t.Name == tag.Name).FirstOrDefaultAsync();

        if (dbTag != null)
            return BadRequest(new { error = "Tag is already created!" });

        var newTag = new Tag()
        {
            Id = Guid.NewGuid(),
            Name = tag.Name,
            CreatedAt = LocalTimeService.LocalTime(),
            UpdatedAt = LocalTimeService.LocalTime()
        };

        await _tags.InsertOneAsync(newTag);
        
        return Ok(new { message = "Tag successful created!" });
    }

    [HttpPut("tag/one-update")]
    public async Task<IActionResult> UpdateTag(Tag tag)
    {
        var dbTag = await _tags.Find(t => t.Id == tag.Id).FirstOrDefaultAsync();

        if (dbTag == null)
            return BadRequest(new {error = "Tag ID is not correct!"});

        var updateTag = Builders<Tag>.Update
            .Set(t => t.Name, tag.Name)
            .Set(t => t.UpdatedAt, DateTime.Now);

        await _tags.FindOneAndUpdateAsync(
            t => t.Id == tag.Id,
            updateTag);

        return Ok(new { message = "Your tag has been updated." });
    }

    [HttpPut("tag/update")]
    public async Task<IActionResult> UpdateTags([FromBody] List<Tag> tags)
    {
        if (tags == null || !tags.Any())
            return BadRequest(new { message = "No tags provided for update." });
        
        
        var updateTags = tags.Select(async tag =>
        {
            var updateDefinition = Builders<Tag>.Update
                .Set(t => t.Name, tag.Name)
                .Set(t => t.UpdatedAt, LocalTimeService.LocalTime());

            await _tags.UpdateOneAsync(t => t.Id == tag.Id, updateDefinition);
        });

        await Task.WhenAll(updateTags);
        
        return Ok();
    }

    [HttpDelete("tag/remove/{tagId}")]
    public async Task<IActionResult> RemoveTag(string tagId)
    {
        var id = Guid.Parse(tagId);

        await _tags.DeleteOneAsync(t => t.Id == id);
        
        return Ok(new { message = "Tag successful removed!" });
    }

    [HttpDelete("tag/remove")]
    public async Task<IActionResult> RemoveTags([FromBody] List<Tag> tags)
    {
        if (tags == null || !tags.Any())
            return BadRequest(new { message = "No tags provided for deletion." });
        
        var tagIds = tags.Select(tag => tag.Id).ToList();

        var result = await _tags.DeleteManyAsync(t => tagIds.Contains(t.Id));
        
        return Ok(new { message = $"{result.DeletedCount} tags were successfully deleted!"});
    }
    
    [HttpDelete("design/remove")]
    public async Task<IActionResult> RemoveTags([FromBody] List<Design> designs)
    {
        if (!await _configService.IsPersonalizationEnabledAsync())
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Personalization module is disabled for this store." });
        if (designs == null || !designs.Any())
            return BadRequest(new { message = "No tags provided for deletion." });
        
        var designIds = designs.Select(ds => ds.Id).ToList();

        var result = await _designs.DeleteManyAsync(d => designIds.Contains(d.Id));
        
        return Ok(new { message = $"{result.DeletedCount} designs were successfully deleted!"});
    }

    [HttpDelete("tag/remove-with-all-products/{tagId}")]
    public async Task<IActionResult> RemoveTagWithHisProducts(string tagId)
    {
        var id = Guid.Parse(tagId);

        var countProducts = await _products.DeleteManyAsync(p => p.TagId == id);

        await _tags.DeleteOneAsync(t => t.Id == id);
        
        return Ok(new { message = $"Tag successful removed with his {countProducts} products!" });
    }
    
    [HttpPost("product/create/{tagId}")]
    public async Task<IActionResult> CreateProduct(Product product, string tagId)
    {
        if (product == null)
            return BadRequest(new { error = "Product is null!" });

        if (tagId == null)
            return BadRequest(new { error = "Tag ID is empty!" });

        var dbTags = await _tags.Find(t => t.Id == Guid.Parse(tagId)).FirstOrDefaultAsync();

        if (dbTags == null)
            return BadRequest(new { error = "No TAG founded!" });

        product.Id = Guid.NewGuid();
        product.TagId = Guid.Parse(tagId);
        product.TagName = dbTags.Name;
        product.CreatedAt = LocalTimeService.LocalTime();

        await _products.InsertOneAsync(product);
        
        return Ok(new { message = "Product successful created!" });
    }

    [HttpPut("product/update")]
    public async Task<IActionResult> UpdateProduct(Product product)
    {
        if (product == null)
            return BadRequest(new { error = "Product is null!" });

        var dbProduct = await _products.Find(p => p.Id == product.Id).FirstOrDefaultAsync();
        
        if (dbProduct == null)
            return NotFound(new { error = "Product not found!" });

        if (dbProduct.Colors.Count == 0 || dbProduct.Colors == null)
        {
            foreach (var color in product.Colors)
            {
                var defaultColors = new List<Colors>
                {
                    new Colors
                    {
                        Name = color.Name,
                        FileId = color.FileId,
                        PathOfFile = color.PathOfFile,
                        Sizes = null
                    }
                };
                
                product.Colors = defaultColors;
            }
        }

        var filterProduct = Builders<Product>.Filter.Eq(p => p.Id, product.Id);
        var resultProduct = await _products.ReplaceOneAsync(filterProduct, product);

        if (resultProduct.MatchedCount == 0)
            return NotFound(new { error = "Product not found!" });
        
        return Ok(new { message = "Your product has been updated." });
    }

    [HttpDelete("product/remove/{productId}")]
    public async Task<IActionResult> RemoveProduct(string productId)
    {
        await _products.DeleteOneAsync(p => p.Id == Guid.Parse(productId));
        
        return Ok(new { message = "Your product has been successful removed!" });
    }
    
    [HttpDelete("product/remove-color/{productId}/{color}")]
    public async Task<IActionResult> RemoveColorForSpecificId(string productId, string color)
    {
        var dbProduct = await _products.Find(p => p.Id == Guid.Parse(productId)).FirstOrDefaultAsync();

        if (dbProduct == null)
            return NotFound(new { error = "Product not found!" });

        var removedCount = dbProduct.Colors.RemoveAll(c => c.Name.Equals(color, StringComparison.OrdinalIgnoreCase));

        if (removedCount > 0)
        {
            var updateResult = await _products.ReplaceOneAsync(p => p.Id == Guid.Parse(productId), dbProduct);

            if (updateResult.ModifiedCount == 0)
                return StatusCode(500, new { error = "Failed to update the product!" });
            
            return Ok(new { message = "Your product has been successful removed!" });
        }
        
        return NotFound(new { error = $"Specified color: {color} not found for this product." });
    }

    [HttpDelete("product/remove-size/{productId}/{color}/{size}")]
    public async Task<IActionResult> RemoveSizeForColorOfSpecificId(string productId, string color, string size)
    {
        var id = Guid.Parse(productId);

        var dbProduct = await _products.Find(p => p.Id == id).FirstOrDefaultAsync();

        if (dbProduct == null)
            return NotFound(new { error = "Product not found!" });

        var dbColor = dbProduct.Colors.FirstOrDefault(c => c.Name.Equals(color, StringComparison.OrdinalIgnoreCase));

        if (dbColor == null)
            return NotFound(new { error = "Specific color not founded for this product!" });

        int removeCount = dbColor.Sizes.RemoveAll(s => s.Size.Equals(size, StringComparison.OrdinalIgnoreCase));

        if (removeCount > 0)
        {
            var updateResult = await _products.ReplaceOneAsync(p => p.Id == id, dbProduct);
            
            if (updateResult.ModifiedCount == 0)
                return StatusCode(500, new { error = "Failed to update the product." });
            
            return Ok(new { message = $"Size {size} has been successfully removed from the product color {color}!" });
        }

        return NotFound(new { error = "Specified size not found for this color." });
    }

    [HttpPost("design/create")]
    public async Task<IActionResult> CreateDesign([FromBody] Design design)
    {
        if (!await _configService.IsPersonalizationEnabledAsync())
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Personalization module is disabled for this store." });

        if (design == null)
            return BadRequest(new {error = "Design is empty!"});

        var dbDesign = _designs.Find(d => d.Name == design.Name).FirstOrDefaultAsync();

        if (dbDesign == null)
            return BadRequest(new { error = "Name of design already exists!"});

        design.Id = Guid.NewGuid();
        design.CreatedAt = LocalTimeService.LocalTime();

        await _designs.InsertOneAsync(design);

        return Ok(new { message = "Design successful created!"});
    }
    
    [HttpPut("design/update")]
    public async Task<IActionResult> UpdateDesign(Design design)
    {
        if (!await _configService.IsPersonalizationEnabledAsync())
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Personalization module is disabled for this store." });

        if (design == null)
            return BadRequest(new { error = "Design is null!" });

        var filterDesign = Builders<Design>.Filter.Eq(d => d.Id, design.Id);
        var resultDesign = await _designs.ReplaceOneAsync(filterDesign, design);

        if (resultDesign.MatchedCount == 0)
            return NotFound(new { error = "Design not found!" });
        
        return Ok(new { message = "Your design has been updated." });
    }

    [HttpDelete("design/custom-delete/{designId}")]
    public async Task<IActionResult> DeleteCustomDesign(string designId)
    {
        if (!await _configService.IsPersonalizationEnabledAsync())
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Personalization module is disabled for this store." });

        var id = Guid.Parse(designId);

        var dbDesign = await _designs.DeleteOneAsync(d => d.Id == id);

        if (dbDesign.DeletedCount == 0)
            return BadRequest(new { error = "No design has been removed!" });

        return Ok(new { message = "Design successful removed!"});
    }
    
    [HttpDelete("design/delete-with-pair/{designId}")]
    public async Task<IActionResult> DeleteCustomDesignWithPair(string designId)
    {
        if (!await _configService.IsPersonalizationEnabledAsync())
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Personalization module is disabled for this store." });
        var id = Guid.Parse(designId);

        var dbDesign = await _designs.DeleteOneAsync(d => d.Id == id);

        if (dbDesign.DeletedCount == 0)
            return BadRequest(new { error = "No design has been removed!" });

        var dbPairDesign = await _pairedDesigns.DeleteManyAsync(d => d.DesignIds.Contains(id));
        
        if (dbPairDesign.DeletedCount == 0)
            return BadRequest(new { error = "No design has been removed!" });

        return Ok(new { message = "Design successful removed!"});
    }

    [HttpPost("design/pair-two-designs")]
    public async Task<IActionResult> PairTwoDesigns(PairedDesign pairedDesign)
    {
        if (!await _configService.IsPersonalizationEnabledAsync())
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Personalization module is disabled for this store." });

        if (pairedDesign == null)
            return BadRequest(new { error =  "Pairing failed!"});

        var dbDesigns = await _designs.Find(d => pairedDesign.DesignIds.Contains(d.Id)).ToListAsync();
        
        var missingDesignIds = pairedDesign.DesignIds.Except(dbDesigns.Select(d => d.Id)).ToList();
        
        if (missingDesignIds.Any())
            return BadRequest(new { error = "One or more designs do not exist!"});

        pairedDesign.Id = Guid.NewGuid();
        pairedDesign.CreatedAt = LocalTimeService.LocalTime();

        await _pairedDesigns.InsertOneAsync(pairedDesign);
        
        return Ok(new {message = "Designs successful paired!"});
    }

    [HttpPut("design/update-two-designs")]
    public async Task<IActionResult> UpdateTwoDesigns(PairedDesign pairedDesign)
    {
        if (!await _configService.IsPersonalizationEnabledAsync())
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Personalization module is disabled for this store." });

        if (pairedDesign == null)
            return BadRequest(new { error =  "Pairing failed!"});
        
        var filterDesign = Builders<PairedDesign>.Filter.Eq(d => d.Id, pairedDesign.Id);
        var resultDesign = await _pairedDesigns.ReplaceOneAsync(filterDesign, pairedDesign);
        
        if (resultDesign.MatchedCount == 0)
            return NotFound(new { error = "Designs not found!" });
        
        return Ok(new { message = "Designs has been updated." });
    }

    [HttpDelete("design/delete-pair-design/{pairDesignId}")]
    public async Task<IActionResult> RemovePairDesign(string pairDesignId)
    {
        if (!await _configService.IsPersonalizationEnabledAsync())
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Personalization module is disabled for this store." });

        var id = Guid.Parse(pairDesignId);

        var dbPairDesign = await _pairedDesigns.DeleteOneAsync(pd => pd.Id == id);

        if (dbPairDesign.DeletedCount == 0)
            return BadRequest("No pair removed!");

        return Ok(new { message = "Pair successful removed!" });
    }
    
    [HttpDelete("design/delete-pair-design")]
    public async Task<IActionResult> RemovePairDesign([FromBody] List<string> pairedDesignIds)
    {
        if (!await _configService.IsPersonalizationEnabledAsync())
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Personalization module is disabled for this store." });

        if (pairedDesignIds == null || !pairedDesignIds.Any())
            return BadRequest(new { message = "No pairedDesign IDs provided for deletion." });
    
        var result = await _pairedDesigns.DeleteManyAsync(pd => pairedDesignIds.Contains(pd.Id.ToString()));
    
        return Ok(new { message = $"{result.DeletedCount} paired designs were successfully deleted!" });
    }

    
    [HttpDelete("design/delete-one-pair/{pairDesignId}/{designId}")]
    public async Task<IActionResult> RemoveOnePairDesign(string pairDesignId, string designId)
    {
        if (!await _configService.IsPersonalizationEnabledAsync())
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Personalization module is disabled for this store." });
        var pairId = Guid.Parse(pairDesignId);
        var dId = Guid.Parse(designId);

        var dbPairDesign = await _pairedDesigns.Find(pd => pd.Id == pairId).FirstOrDefaultAsync();

        var removedCount = dbPairDesign.DesignIds.RemoveAll(d => d.Equals(dId));

        if (removedCount > 0)
        {
            var updateResult = await _pairedDesigns.ReplaceOneAsync(pd => pd.Id == pairId, dbPairDesign);
            
            if (updateResult.ModifiedCount == 0)
                return StatusCode(500, new { error = "Failed to update the pair design." });
            
            return Ok(new { message = "Pair successful removed!" });
        }

        return BadRequest(new { error = "Any pair removed!" });
    }
    
    //Customizations
    [HttpGet("customizations")]
    public async Task<IActionResult> GetAllCustomizations()
    {
        var dbCustomizations = await _customizations.Find(_ => true).ToListAsync();

        if (dbCustomizations == null || dbCustomizations.Count == 0)
            return NotFound(new { error = "No Customizations found!" });
        
        var designIds = dbCustomizations
            .Select(c => c.DesignId)
            .Distinct()
            .ToList();

        var designs = await _designs.Find(
            d => designIds.Contains(d.Id.ToString()))
            .ToListAsync();
        
        var productIds = dbCustomizations
            .Select(c => c.ProductId)
            .Distinct()
            .ToList();

        var products = await _products.Find(
                p => productIds.Contains(p.Id.ToString()))
            .ToListAsync();

        return Ok(new
        {
            Customization = dbCustomizations,
            Design = designs,
            Product = products
        });
    }
    
    //Orders
    [HttpGet("orders")]
    public async Task<IActionResult> GetAllOrders()
    {
        var dbOrders = await _orderStore.GetAllAsync();

        if (dbOrders == null || dbOrders.Count == 0)
            return NotFound(new { error = "No Orders designs!" });

        var customizationIds = dbOrders
            .SelectMany(o => o.Customizations)
            .Distinct()
            .ToList();

        var dbCustomizations = await _customizations
            .Find(c => customizationIds.Contains(c.Id))
            .ToListAsync();

        return Ok(new
        {
            Orders = dbOrders,
            Customization = dbCustomizations
        });
    }
    
    [HttpPost("orders/increase-status/{orderId}")]
    public async Task<IActionResult> IncreaseOrderStatus(int orderId)
    {
        var result = await _orderLifecycleService.IncreaseStatusAsync(orderId, OrderActor.Staff());
        if (!result.IsSuccess)
        {
            return result.Status switch
            {
                OrderMutationStatus.NotFound => NotFound(new { error = "Order not found!" }),
                _ => BadRequest(new { error = result.Message })
            };
        }

        return Ok(new { message = "Order status increased!", order = result.Order });
    }
    
    [HttpPost("orders/decrease-status/{orderId}")]
    public async Task<IActionResult> DecreaseOrderStatus(int orderId)
    {
        var result = await _orderLifecycleService.DecreaseStatusAsync(orderId, OrderActor.Staff());
        if (!result.IsSuccess)
        {
            return result.Status switch
            {
                OrderMutationStatus.NotFound => NotFound(new { error = "Order not found!" }),
                _ => BadRequest(new { error = result.Message })
            };
        }

        return Ok(new { message = "Order status decreased!", order = result.Order });
    }

    [HttpDelete("orders/{orderId}")]
    public Task<IActionResult> RemoveOrder(int orderId)
    {
        return Task.FromResult<IActionResult>(BadRequest(new { error = "Orders cannot be deleted; use cancellation instead." }));
    }

    [HttpPost("orders/cancel/{orderId}")]
    public async Task<IActionResult> CancelOrder(int orderId)
    {
        var result = await _orderLifecycleService.CancelOrderAsync(orderId, OrderActor.Staff());
        if (!result.IsSuccess)
            return NotFound(new { error = "Order not found or already cancelled!" });

        return Ok(new { message = "Order cancelled!", order = result.Order });
    }
    
    [HttpPut("orders/{orderId}")]
    public async Task<IActionResult> UpdateOrder(int orderId, [FromBody] AdminUpdateOrderRequest updatedOrder)
    {
        if (updatedOrder == null)
            return BadRequest(new { error = "Invalid order data!" });

        var existingOrder = await _orderStore.GetByIdAsync(orderId);
        if (existingOrder == null)
            return NotFound(new { error = "Order not found!" });

        var result = await _orderLifecycleService.UpdateOrderDetailsAsync(
            orderId,
            updatedOrder.DeliveryMethod ?? existingOrder.DeliveryMethod ?? "HomeDelivery",
            updatedOrder.PacketaPointId,
            updatedOrder.PacketaPointName,
            updatedOrder.PacketaPointAddress,
            updatedOrder.StatusOrder,
            OrderActor.Staff());

        if (!result.IsSuccess)
        {
            return result.Status switch
            {
                OrderMutationStatus.NotFound => NotFound(new { error = "Order not found!" }),
                _ => BadRequest(new { error = result.Message })
            };
        }

        return Ok(new { message = "Order updated!", updatedOrder });
    }
    
    [HttpGet("orders/{orderId}")]
    public async Task<IActionResult> GetOrderInformation(int orderId)
    {
        var order = await _orderStore.GetByIdAsync(orderId);
        if (order == null)
            return NotFound(new { error = "Order not found" });

        if (OrderSnapshotPresentation.HasSnapshots(order))
        {
            var (snapshotCustomizations, snapshotProducts, snapshotDesigns) = OrderSnapshotPresentation.MaterializeDetails(order);
            return Ok(new { order, customizations = snapshotCustomizations, products = snapshotProducts, designs = snapshotDesigns });
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

        return Ok(new { order, customizations, products = filteredProducts, designs });
    }
    
    [HttpPost("design/getSpecific")]
    public async Task<IActionResult> GetSpecificDesigns([FromBody] List<string> designIds)
    {
        if (!await _configService.IsPersonalizationEnabledAsync())
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Personalization module is disabled for this store." });

        if (designIds == null || !designIds.Any())
            return BadRequest(new { error = "Žiadne ID dizajnov neboli poskytnuté." });
    
        var guidIds = designIds.Select(id => Guid.Parse(id)).ToList();
        var designs = await _designs.Find(d => guidIds.Contains(d.Id)).ToListAsync();

        if (designs == null || designs.Count == 0)
            return NotFound(new { error = "Dizajny neboli nájdené." });
    
        return Ok(designs);
    }

    [HttpGet("orders/sales-summary")]
    public async Task<IActionResult> GetSalesSummary()
    {
        var summary = await _orderStore.GetSalesSummaryAsync();
        return Ok(summary);
    }

    [HttpGet("products/low-stock")]
    public async Task<IActionResult> GetLowStockProducts()
    {
        int threshold = 2;
        var allProducts = await _products.Find(_ => true).ToListAsync();
        var lowStockItems = new List<object>();

        foreach (var product in allProducts)
        {
            foreach (var color in product.Colors)
            {
                foreach (var size in color.Sizes)
                {
                    if (size.Quantity < threshold)
                    {
                        lowStockItems.Add(new
                        {
                            productName = product.Name,
                            color = color.Name,
                            size = size.Size,
                            quantity = size.Quantity
                        });
                    }
                }
            }
        }
    
        return Ok(lowStockItems);
    }
    
    [HttpGet("kpi")]
    public async Task<IActionResult> GetKpiData()
    {
        var kpi = await _orderStore.GetKpiDataAsync();
        return Ok(kpi);
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserInformation(string userId)
    {
        var _userId = Guid.Parse(userId);

        var dbUser = await _users.Find(u => u.Id == _userId).FirstOrDefaultAsync();

        if (dbUser != null)
        {
            return Ok(new {
                userType = "Normal",
                data     = UserResponse.From(dbUser)
            });
        }

        var guestUser = await _guestUsers
            .Find(g => g.Id == _userId)
            .FirstOrDefaultAsync();

        if (guestUser != null)
        {
            return Ok(new {
                userType = "Guest",
                data     = guestUser
            });
        }

        return NotFound(new { error = "User not found" });
    }

    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings()
    {
        var settings = await _configService.GetConfigurationAsync();
        return Ok(settings);
    }

    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateSettingsRequest request)
    {
        if (request.CashOnDeliveryFee.HasValue && request.CashOnDeliveryFee.Value < 0)
            return BadRequest(new { error = "Poplatok za dobierku nemôže byť záporný." });
        if (request.HomeDeliveryFee.HasValue && request.HomeDeliveryFee.Value < 0)
            return BadRequest(new { error = "Poplatok za doručenie nemôže byť záporný." });
        if (request.PacketaFee.HasValue && request.PacketaFee.Value < 0)
            return BadRequest(new { error = "Poplatok za Packetu nemôže byť záporný." });

        var current = await _configService.GetConfigurationAsync();

        if (request.StoreName != null) current.StoreName = request.StoreName;
        if (request.LogoUrl != null) current.LogoUrl = request.LogoUrl;
        if (request.ContactEmail != null) current.ContactEmail = request.ContactEmail;
        if (request.ContactPhone != null) current.ContactPhone = request.ContactPhone;
        if (request.Currency != null) current.Currency = request.Currency;
        if (request.DefaultLocale != null) current.DefaultLocale = request.DefaultLocale;

        if (request.EnablePersonalization.HasValue) current.EnablePersonalization = request.EnablePersonalization.Value;
        if (request.EnableReviews.HasValue) current.EnableReviews = request.EnableReviews.Value;
        if (request.EnableCoupons.HasValue) current.EnableCoupons = request.EnableCoupons.Value;
        if (request.EnableAdvancedReporting.HasValue) current.EnableAdvancedReporting = request.EnableAdvancedReporting.Value;

        if (request.EnableStripe.HasValue) current.EnableStripe = request.EnableStripe.Value;
        if (request.EnableCashOnDelivery.HasValue) current.EnableCashOnDelivery = request.EnableCashOnDelivery.Value;
        if (request.CashOnDeliveryFee.HasValue) current.CashOnDeliveryFee = request.CashOnDeliveryFee.Value;

        if (request.EnableBankTransfer.HasValue) current.EnableBankTransfer = request.EnableBankTransfer.Value;
        if (request.BankAccountIban != null) current.BankAccountIban = request.BankAccountIban;
        if (request.BankAccountBic != null) current.BankAccountBic = request.BankAccountBic;
        if (request.BankTransferInstructions != null) current.BankTransferInstructions = request.BankTransferInstructions;

        if (request.EnableHomeDelivery.HasValue) current.EnableHomeDelivery = request.EnableHomeDelivery.Value;
        if (request.HomeDeliveryFee.HasValue) current.HomeDeliveryFee = request.HomeDeliveryFee.Value;

        if (request.EnablePacketa.HasValue) current.EnablePacketa = request.EnablePacketa.Value;
        if (request.PacketaApiKey != null) current.PacketaApiKey = request.PacketaApiKey;
        if (request.PacketaFee.HasValue) current.PacketaFee = request.PacketaFee.Value;

        if (request.Timezone != null) current.Timezone = request.Timezone;
        if (request.VatPayer.HasValue) current.VatPayer = request.VatPayer.Value;
        if (request.VatRate.HasValue) current.VatRate = request.VatRate.Value;
        if (request.CompanyRegistrationNumber != null) current.CompanyRegistrationNumber = request.CompanyRegistrationNumber;
        if (request.TaxRegistrationNumber != null) current.TaxRegistrationNumber = request.TaxRegistrationNumber;
        if (request.VatRegistrationNumber != null) current.VatRegistrationNumber = request.VatRegistrationNumber;
        if (request.BillingAddress != null) current.BillingAddress = request.BillingAddress;

        var saved = await _configService.UpdateConfigurationAsync(current);
        return Ok(new { message = "Nastavenia boli úspešne uložené.", settings = saved });
    }

    [HttpPost("orders/mark-paid/{orderId}")]
    public async Task<IActionResult> MarkOrderAsPaid(int orderId)
    {
        var result = await _orderLifecycleService.MarkPaidAsync(orderId, OrderActor.Staff());
        if (!result.IsSuccess)
        {
            return result.Status switch
            {
                OrderMutationStatus.NotFound => NotFound(new { error = "Order not found!" }),
                _ => BadRequest(new { error = result.Message })
            };
        }

        return Ok(new { message = "Platba objednávky bola úspešne potvrdená.", order = result.Order });
    }
}

public class UpdateSettingsRequest
{
    // Legacy fields
    public decimal? CashOnDeliveryFee { get; set; }
    public string? PacketaApiKey { get; set; }

    // Store profile
    public string? StoreName { get; set; }
    public string? LogoUrl { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? Currency { get; set; }
    public string? DefaultLocale { get; set; }

    // Module entitlements
    public bool? EnablePersonalization { get; set; }
    public bool? EnableReviews { get; set; }
    public bool? EnableCoupons { get; set; }
    public bool? EnableAdvancedReporting { get; set; }

    // Payment methods
    public bool? EnableStripe { get; set; }
    public bool? EnableCashOnDelivery { get; set; }
    public bool? EnableBankTransfer { get; set; }
    public string? BankAccountIban { get; set; }
    public string? BankAccountBic { get; set; }
    public string? BankTransferInstructions { get; set; }

    // Delivery methods
    public bool? EnableHomeDelivery { get; set; }
    public decimal? HomeDeliveryFee { get; set; }
    public bool? EnablePacketa { get; set; }
    public decimal? PacketaFee { get; set; }

    // Merchant operations
    public string? Timezone { get; set; }
    public bool? VatPayer { get; set; }
    public decimal? VatRate { get; set; }
    public string? CompanyRegistrationNumber { get; set; }
    public string? TaxRegistrationNumber { get; set; }
    public string? VatRegistrationNumber { get; set; }
    public string? BillingAddress { get; set; }
}
