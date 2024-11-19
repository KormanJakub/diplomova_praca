using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using nia_api.Data;
using nia_api.Models;

namespace nia_api.Controllers;

[ApiController]
[Route("payment")]
public class PaymentController : ControllerBase
{
    private readonly IMongoCollection<Product> _products;

    public PaymentController(NiaDbContext context)
    {
        _products = context.Products;
    }
    
    

}