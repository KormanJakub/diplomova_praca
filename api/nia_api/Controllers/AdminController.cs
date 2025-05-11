using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using nia_api.Data;
using nia_api.Enums;
using nia_api.Models;
using nia_api.Services;
using Tag = nia_api.Models.Tag;

namespace nia_api.Controllers;

[ApiController]
[Route("admin")]
public class AdminController : ControllerBase
{
    private readonly IMongoCollection<Design> _designs;
    private readonly IMongoCollection<Product> _products;
    private readonly IMongoCollection<Tag> _tags;
    private readonly IMongoCollection<PairedDesign> _pairedDesigns;
    private readonly IMongoCollection<Customization> _customizations;
    private readonly IMongoCollection<Order> _orders;
    private readonly IMongoCollection<User> _users;
    private readonly IMongoCollection<GuestUser> _guestUsers;
    
    public AdminController(NiaDbContext context)
    {
        _designs = context.Designs;
        _products = context.Products;
        _tags = context.Tags;
        _pairedDesigns = context.PairedDesigns;
        _customizations = context.Customizations;
        _orders = context.Orders;
        _users = context.Users;
        _guestUsers = context.GuestUsers;
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
        var dbDesigns = await _designs.Find(_ => true).ToListAsync();

        if (dbDesigns == null || dbDesigns.Count == 0)
            return NotFound(new { error = "No designs found!"});

        return Ok(dbDesigns);
    }

    [HttpGet("design/all-paired-designs")]
    public async Task<IActionResult> GetAllPairedDesgings()
    {
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
        var id = Guid.Parse(designId);

        var dbDesign = await _designs.DeleteOneAsync(d => d.Id == id);

        if (dbDesign.DeletedCount == 0)
            return BadRequest(new { error = "No design has been removed!" });

        return Ok(new { message = "Design successful removed!"});
    }
    
    [HttpDelete("design/delete-with-pair/{designId}")]
    public async Task<IActionResult> DeleteCustomDesignWithPair(string designId)
    {
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
        var id = Guid.Parse(pairDesignId);

        var dbPairDesign = await _pairedDesigns.DeleteOneAsync(pd => pd.Id == id);

        if (dbPairDesign.DeletedCount == 0)
            return BadRequest("No pair removed!");

        return Ok(new { message = "Pair successful removed!" });
    }
    
    [HttpDelete("design/delete-pair-design")]
    public async Task<IActionResult> RemovePairDesign([FromBody] List<string> pairedDesignIds)
    {
        if (pairedDesignIds == null || !pairedDesignIds.Any())
            return BadRequest(new { message = "No pairedDesign IDs provided for deletion." });
    
        var result = await _pairedDesigns.DeleteManyAsync(pd => pairedDesignIds.Contains(pd.Id.ToString()));
    
        return Ok(new { message = $"{result.DeletedCount} paired designs were successfully deleted!" });
    }

    
    [HttpDelete("design/delete-one-pair/{pairDesignId}/{designId}")]
    public async Task<IActionResult> RemoveOnePairDesign(string pairDesignId, string designId)
    {
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
        var dbOrders = await _orders.Find(_ => true).ToListAsync();

        if (dbOrders.Count == 0 || dbOrders == null)
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
        var order = await _orders.Find(o => o.Id == orderId).FirstOrDefaultAsync();
        if (order == null)
            return NotFound(new { error = "Order not found!" });

        if (order.StatusOrder < EStatus.ZRUSENA) 
            order.StatusOrder++;

        await _orders.ReplaceOneAsync(o => o.Id == orderId, order);

        return Ok(new { message = "Order status increased!", order });
    }
    
    [HttpPost("orders/decrease-status/{orderId}")]
    public async Task<IActionResult> DecreaseOrderStatus(int orderId)
    {
        var order = await _orders.Find(o => o.Id == orderId).FirstOrDefaultAsync();
        if (order == null)
            return NotFound(new { error = "Order not found!" });

        if (order.StatusOrder > EStatus.PRIJATA)
            order.StatusOrder--;

        await _orders.ReplaceOneAsync(o => o.Id == orderId, order);

        return Ok(new { message = "Order status decreased!", order });
    }

    [HttpDelete("orders/{orderId}")]
    public async Task<IActionResult> RemoveOrder(int orderId)
    {
        var result = await _orders.DeleteOneAsync(o => o.Id == orderId);

        if (result.DeletedCount == 0)
            return NotFound(new { error = "Order not found!" });

        return Ok(new { message = "Order deleted successfully!" });
    }

    [HttpPost("orders/cancel/{orderId}")]
    public async Task<IActionResult> CancelOrder(int orderId)
    {
        var order = await _orders.Find(o => o.Id == orderId).FirstOrDefaultAsync();
        if (order == null)
            return NotFound(new { error = "Order not found!" });

        order.StatusOrder = EStatus.ZRUSENA;
        await _orders.ReplaceOneAsync(o => o.Id == orderId, order);

        return Ok(new { message = "Order cancelled!", order });
    }
    
    [HttpPut("orders/{orderId}")]
    public async Task<IActionResult> UpdateOrder(int orderId, [FromBody] Order updatedOrder)
    {
        if (updatedOrder == null)
            return BadRequest(new { error = "Invalid order data!" });

        var existingOrder = await _orders.Find(o => o.Id == orderId).FirstOrDefaultAsync();
        if (existingOrder == null)
            return NotFound(new { error = "Order not found!" });

        updatedOrder.Id = orderId;
        await _orders.ReplaceOneAsync(o => o.Id == orderId, updatedOrder);

        return Ok(new { message = "Order updated!", updatedOrder });
    }
    
    [HttpGet("orders/{orderId}")]
    public async Task<IActionResult> GetOrderInformation(int orderId)
    {
        var order = await _orders.Find(o => o.Id == orderId).FirstOrDefaultAsync();
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

        return Ok(new { order, customizations, products = filteredProducts, designs });
    }
    
    [HttpPost("design/getSpecific")]
    public async Task<IActionResult> GetSpecificDesigns([FromBody] List<string> designIds)
    {
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
        var soldCount = await _orders.CountDocumentsAsync(o => o.StatusOrder == EStatus.ZAPLATENA);
        var pendingCount = await _orders.CountDocumentsAsync(o => o.StatusOrder == EStatus.PRIJATA);
        var makingCount = await _orders.CountDocumentsAsync(o => o.StatusOrder == EStatus.VO_VYROBE);
        var readyCount = await _orders.CountDocumentsAsync(o => o.StatusOrder == EStatus.PRIPRAVENA);
        var sendCount = await _orders.CountDocumentsAsync(o => o.StatusOrder == EStatus.POSLANA);
        var cancelCount = await _orders.CountDocumentsAsync(o => o.StatusOrder == EStatus.ZRUSENA);
    
        return Ok(new 
        { 
            soldOrders = soldCount,
            pendingOrders = pendingCount,
            makingOrders = makingCount,
            readyOrders = readyCount,
            sendOrders = sendCount,
            cancelOrders = cancelCount
        });
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
        var filter = Builders<Order>.Filter.Ne(o => o.StatusOrder, EStatus.ZRUSENA);
    
        var totalOrders = await _orders.CountDocumentsAsync(filter);

        var revenueAggregate = await _orders.Aggregate()
            .Match(filter)
            .Group(new BsonDocument 
            { 
                { "_id", BsonNull.Value }, 
                { "totalRevenue", new BsonDocument("$sum", "$totalPrice") }
            })
            .FirstOrDefaultAsync();
        
        decimal totalRevenue = revenueAggregate != null ? revenueAggregate["totalRevenue"].ToDecimal() : 0;
        decimal averageOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;

        var distinctUserIds = await _orders.DistinctAsync<Guid>("UserId", filter);
        int newCustomers = distinctUserIds.ToList().Count;

        return Ok(new 
        {
            totalOrders,
            totalRevenue,
            averageOrderValue,
            newCustomers
        });
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
                data     = dbUser
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
}