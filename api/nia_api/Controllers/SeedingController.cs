using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using nia_api.Data;
using nia_api.Models;
using nia_api.Services;
using Tag = nia_api.Models.Tag;

namespace nia_api.Controllers;

[ApiController]
[Route("seed")]
public class SeedingController : ControllerBase
{
    private readonly IMongoCollection<Design> _designs;
    private readonly IMongoCollection<Product> _products;
    private readonly IMongoCollection<Tag> _tags;
    private readonly IMongoCollection<PairedDesign> _pairedDesigns;
    
    public SeedingController(NiaDbContext context)
    {
        _designs = context.Designs;
        _products = context.Products;
        _tags = context.Tags;
        _pairedDesigns = context.PairedDesigns;
    }
    
    [HttpGet("data")]
    public async Task SeedDatabase()
    {
        await _designs.DeleteManyAsync(FilterDefinition<Design>.Empty);
        await _products.DeleteManyAsync(FilterDefinition<Product>.Empty);
        await _pairedDesigns.DeleteManyAsync(FilterDefinition<PairedDesign>.Empty);
        await _tags.DeleteManyAsync(FilterDefinition<Tag>.Empty);

        //TAGY
        var tag1 = new Tag
        {
            Id = Guid.Parse("82b30c6b-ffe3-4a62-a7f4-2d9587cc44f0"),
            Name = "Mikiny bez kapucní",
            CreatedAt = LocalTimeService.LocalTime()
        };
        
        var tag2 = new Tag
        {
            Id = Guid.Parse("c9415da6-10b3-4b97-ae41-9b92e63a05b3"),
            Name = "Tričká",
            CreatedAt = LocalTimeService.LocalTime()
        };
        
        var tag3 = new Tag
        {
            Id = Guid.Parse("0d3a5093-9125-4c23-bee8-e9d97052cb5f"),
            Name = "Mikiny s kapucňami",
            CreatedAt = LocalTimeService.LocalTime()
        };

        await _tags.InsertManyAsync(new List<Tag> { tag1, tag2, tag3 });
        
        //PRODUKTY
        var product1 = new Product
        {
            Id = Guid.Parse("924966f0-bf03-4ac4-8dbe-001a9d0593ef"),
            Name = "18500B HEAVY BLEND™ YOUTH hooded mikina",
            TagId = Guid.Parse("0d3a5093-9125-4c23-bee8-e9d97052cb5f"),
            Description = "279 g/m² (White: 265 g/m²), 50% bavlan, 50% polyester",
            Price = 11.95M,
            Colors = new List<Colors>
            {
                new Colors
                {
                    Name = "Biela",
                    Sizes = new List<SizeInfo>
                    {
                        new SizeInfo { Size = "S", Quantity = 10},
                        new SizeInfo { Size = "M", Quantity = 5},
                        new SizeInfo { Size = "L", Quantity = 8}
                    }
                },
                new Colors
                {
                    Name = "Čierna",
                    Sizes = new List<SizeInfo>
                    {
                        new SizeInfo { Size = "S", Quantity = 10},
                        new SizeInfo { Size = "M", Quantity = 5},
                        new SizeInfo { Size = "L", Quantity = 8},
                        new SizeInfo { Size = "XL", Quantity = 2}
                    }
                },
                new Colors
                {
                    Name = "Zelená irish",
                    Sizes = new List<SizeInfo>
                    {
                        new SizeInfo { Size = "S", Quantity = 1},
                        new SizeInfo { Size = "M", Quantity = 2},
                        new SizeInfo { Size = "L", Quantity = 0}
                    }
                }
            },
            CreatedAt = LocalTimeService.LocalTime()
        };
        
        var product2 = new Product
        {
            Id = Guid.Parse("38e15598-8148-4381-8aee-c311ce61ed2f"),
            Name = "18000 HEAVY BLEND™ ADULT crewneck mikina",
            TagId = Guid.Parse("82b30c6b-ffe3-4a62-a7f4-2d9587cc44f0"),
            Description = "270 g/m² (Biele: 255 g/m²), 50% bavlan, 50% polyester",
            Price = 12.80M,
            Colors = new List<Colors>
            {
                new Colors
                {
                    Name = "Tmavá Heather",
                    Sizes = new List<SizeInfo>
                    {
                        new SizeInfo { Size = "S", Quantity = 1},
                        new SizeInfo { Size = "M", Quantity = 1},
                        new SizeInfo { Size = "L", Quantity = 0}
                    }
                },
                new Colors
                {
                    Name = "Biela",
                    Sizes = new List<SizeInfo>
                    {
                        new SizeInfo { Size = "XL", Quantity = 1},
                    }
                }
            },
            CreatedAt = LocalTimeService.LocalTime()
        };
        
        var product3 = new Product
        {
            Id = Guid.Parse("47c7cb92-1d66-4078-9220-0fa55086add3"),
            Name = "2000 ULTRA COTTON™ ADULT tričko",
            TagId = Guid.Parse("c9415da6-10b3-4b97-ae41-9b92e63a05b3"),
            Description = "279 g/m² (White: 265 g/m²), 100% bavlna (Ash: 99% bavlna, 1% polyester; Sport Grey: 90% bavlna, 10% polyester)\n",
            Price = 4.90M,
            Colors = new List<Colors>
            {
                new Colors
                {
                    Name = "Natural",
                    Sizes = new List<SizeInfo>
                    {
                        new SizeInfo { Size = "S", Quantity = 12},
                        new SizeInfo { Size = "M", Quantity = 0},
                        new SizeInfo { Size = "L", Quantity = 3},
                        new SizeInfo { Size = "XL", Quantity = 10},
                        new SizeInfo { Size = "3XL", Quantity = 2},
                        new SizeInfo { Size = "5XL", Quantity = 1}
                    }
                }
            },
            CreatedAt = LocalTimeService.LocalTime()
        };
        
        var product4 = new Product
        {
            Id = Guid.Parse("baa6bae4-ab93-4ec5-8cb5-8eb39c51ff02"),
            Name = "18000B HEAVY BLEND™ YOUTH crewneck mikina",
            TagId = Guid.Parse("82b30c6b-ffe3-4a62-a7f4-2d9587cc44f0"),
            Description = "270 g/m² (Biele: 255 g/m²), 50% bavlan, 50% polyester, priadza Open-End",
            Price = 7.37M,
            Colors = new List<Colors>
            {
                new Colors
                {
                    Name = "Modrá royal",
                    Sizes = new List<SizeInfo>
                    {
                    }
                }
            },
            CreatedAt = LocalTimeService.LocalTime()
        };
        
        var product5 = new Product
        {
            Id = Guid.Parse("1ecb389a-4399-472e-a348-9811b4e9cc23"),
            Name = "18500B HEAVY BLEND™ YOUTH hooded mikina",
            TagId = Guid.Parse("0d3a5093-9125-4c23-bee8-e9d97052cb5f"),
            Description = "279 g/m² (White: 265 g/m²), 50% bavlan, 50% polyester, farby heather sport: 60% polyester, 40% bavlna",
            Price = 11.95M,
            Colors = new List<Colors>
            {
                new Colors
                {
                }
            },
            CreatedAt = LocalTimeService.LocalTime()
        };

        await _products.InsertManyAsync(new List<Product> { product1, product2, product3, product4, product5 });
        
        //DESIGNY
        var design1 = new Design
        {
            Id = Guid.Parse("72c71225-b5a2-487b-804d-78d4d33bfffc"),
            Name = "Mikey Mouse",
            Price = 5M,
            CreatedAt = LocalTimeService.LocalTime()
        };
        
        var design2 = new Design
        {
            Id = Guid.Parse("8ec40a4c-344f-4700-aad6-33dc3eba4771"),
            Name = "Car 1",
            Price = 7M,
            CreatedAt = LocalTimeService.LocalTime()
        };
        
        var design3 = new Design
        {
            Id = Guid.Parse("c23fd540-bd37-4c16-baf5-7dde0a950cec"),
            Name = "Car 2",
            Price = 7M,
            CreatedAt = LocalTimeService.LocalTime()
        };
        
        var design4 = new Design
        {
            Id = Guid.Parse("b8a7375a-e0c3-489e-b4c0-1f2a0b4eb9d2"),
            Name = "Miney Mouse",
            Price = 5M,
            CreatedAt = LocalTimeService.LocalTime()
        };

        await _designs.InsertManyAsync(new List<Design> { design1, design2, design3, design4 });
        
        //PAIRED DESIGN
        var pair1 = new PairedDesign
        {
            Id = Guid.Parse("cef33b89-4435-44fd-9bb1-4e7e742fee75"),
            Name = "Mickey a Miney",
            DesignIds = new List<Guid>
            {
                Guid.Parse("72c71225-b5a2-487b-804d-78d4d33bfffc"),
                Guid.Parse("b8a7375a-e0c3-489e-b4c0-1f2a0b4eb9d2")
            },
            CreatedAt = LocalTimeService.LocalTime()
        };
        
        var pair2 = new PairedDesign
        {
            Id = Guid.Parse("5a6472f9-3b8a-44c5-a3b3-c06ffe91af0d"),
            Name = "Autičká",
            DesignIds = new List<Guid>
            {
                Guid.Parse("8ec40a4c-344f-4700-aad6-33dc3eba4771"),
                Guid.Parse("c23fd540-bd37-4c16-baf5-7dde0a950cec")
            },
            CreatedAt = LocalTimeService.LocalTime()
        };

        await _pairedDesigns.InsertManyAsync(new List<PairedDesign> { pair1, pair2 });
        
        Console.WriteLine("Database seeded with test data.");
    }
}