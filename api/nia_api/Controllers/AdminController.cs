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

    [HttpGet("design/paired-design/designs-in-pair/{pairedDesignId}")]
    public async Task<IActionResult> GetDesignsInPair(string pairedDesignId)
    {
        // Parse the pairedDesignId to a Guid
        Guid parsedPairedDesignId;
        if (!Guid.TryParse(pairedDesignId, out parsedPairedDesignId))
        {
            return BadRequest(new { error = "Invalid pairedDesignId format." });
        }

        // Retrieve the paired design document
        var dbPairedDesigns = await _pairedDesigns
            .Find(pd => pd.Id == parsedPairedDesignId)
            .FirstOrDefaultAsync();

        // Check if the paired design document exists
        if (dbPairedDesigns == null)
        {
            return NotFound(new { error = "Paired design not found." });
        }

        Console.WriteLine(dbPairedDesigns); // Debugging output

        // Check if DesignIds is not null or empty
        if (dbPairedDesigns.DesignIds == null || !dbPairedDesigns.DesignIds.Any())
        {
            return NotFound(new { error = "No designs found for this paired design." });
        }

        // Retrieve the designs associated with the DesignIds
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
            CreatedAt = tag.CreatedAt,
            UpdatedAt = tag.UpdatedAt
        };

        await _tags.InsertOneAsync(newTag);
        
        return Ok(new { message = "Tag successful created!" });
    }

    [HttpPut("tag/update")]
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
    
    [HttpPost("product/create/{tagId}")]
    public async Task<IActionResult> CreateProduct(Product product, string tagId)
    {
        if (product == null)
            return BadRequest(new { error = "Product is null!" });

        if (tagId == null)
            return BadRequest(new { error = "Tag ID is empty!" });

        var dbTags = _tags.Find(t => t.Id == Guid.Parse(tagId)).FirstOrDefaultAsync();

        if (dbTags == null)
            return BadRequest(new { error = "No TAG founded!" });

        product.Id = Guid.NewGuid();
        product.TagId = Guid.Parse(tagId);
        product.CreatedAt = LocalTimeService.LocalTime();

        await _products.InsertOneAsync(product);
        
        return Ok(new { message = "Product successful created!" });
    }

    [HttpPut("product/update")]
    public async Task<IActionResult> UpdateProduct(Product product)
    {
        if (product == null)
            return BadRequest(new { error = "Product is null!" });

        var filterProduct = Builders<Product>.Filter.Eq(p => p.Id, product.Id);
        var resultProduct = await _products.ReplaceOneAsync(filterProduct, product);

        if (resultProduct.MatchedCount == 0)
            return NotFound(new { error = "Product not found!" });
        
        return Ok(new { message = "Your product has been updated." });
    }

    [HttpPost("design/create")]
    public async Task<IActionResult> CreateDesign(Design design)
    {
        if (design == null)
            return BadRequest(new {error = "Design is empty!"});

        var dbDesign = _designs.Find(d => d.Name == design.Name).FirstOrDefaultAsync();

        if (dbDesign != null)
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
    
    //TODO: For all collections I need to make remove...
}