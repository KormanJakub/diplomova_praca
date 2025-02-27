using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using nia_api.Data;
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
    
    public AdminController(NiaDbContext context)
    {
        _designs = context.Designs;
        _products = context.Products;
        _tags = context.Tags;
        _pairedDesigns = context.PairedDesigns;
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
}