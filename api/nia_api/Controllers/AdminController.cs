using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using nia_api.Data;
using nia_api.Models;
using Tag = nia_api.Models.Tag;

namespace nia_api.Controllers;

/*
 * TODO: ESTE ORDRES - nemôže ich updatovať a ani vymazať
 * GET: All products
 * GET: All tags
 * GET: All designs
 * POST: New product
 * POST: New tag
 * POST: New design
 * PUT: Update product
 * PUT: Update tag
 * PUT: Update design
 * DELETE: Products
 * DELETE: Tag
 * DELETE: Design
 */

[ApiController]
[Route("admin")]
public class AdminController : ControllerBase
{
    private readonly IMongoCollection<Design> _designs;
    private readonly IMongoCollection<Product> _products;
    private readonly IMongoCollection<Tag> _tags;
    
    public AdminController(NiaDbContext context)
    {
        _designs = context.Designs;
        _products = context.Products;
        _tags = context.Tags;
    }
    
    
}