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
    private readonly IMongoCollection<Gallery> _gallery;
    
    public SeedingController(NiaDbContext context)
    {
        _designs = context.Designs;
        _products = context.Products;
        _tags = context.Tags;
        _pairedDesigns = context.PairedDesigns;
        _gallery = context.Gallery;
    }
    
    [HttpGet("data")]
    public async Task SeedDatabase()
    {
        await _designs.DeleteManyAsync(FilterDefinition<Design>.Empty);
        await _products.DeleteManyAsync(FilterDefinition<Product>.Empty);
        await _pairedDesigns.DeleteManyAsync(FilterDefinition<PairedDesign>.Empty);
        await _tags.DeleteManyAsync(FilterDefinition<Tag>.Empty);
        await _gallery.DeleteManyAsync(FilterDefinition<Gallery>.Empty);

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
                    FileId = "9604dad2-082d-4930-a0d5-ca2cd38a1f17",
                    PathOfFile = "/Files/G18500K_White.jpg",
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
                    FileId = "0597112e-183b-465e-a6cb-1f4a920a14ea",
                    PathOfFile = "/Files/G18500K_Black.jpg",
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
                    FileId = "9d5c41a2-c92c-4afa-bfee-54a6130b623f",
                    PathOfFile = "/Files/G18500K_Irish-Green.jpg",
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
                    FileId = "b0eb156a-6235-4997-8e50-4b804e3797e8",
                    PathOfFile = "/Files/G18500K_Sport-Grey-(Heather).jpg",
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
                    FileId = "03fca15c-05dd-4192-89ad-5e606e42fa58",
                    PathOfFile = "/Files/G18500K_Maroon.jpg",
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
                    FileId = "a8e4b0bc-0391-442e-9b48-b147c5a7e2f9",
                    PathOfFile = "/Files/G18500K_Graphite-Heather.jpg",
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
                    FileId = "46bda27a-64b3-40fa-baf9-f68a155dc467",
                    PathOfFile = "/Files/G18500K_Royal.jpg",
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
        
        //GALERY
        var gallery1 = new Gallery()
        {
            Id = Guid.NewGuid(),
            FileId = "58b1e48d-209a-4856-8390-1310775ea2ae",
            PathOfFile = "/Files/Photo_Gallery_1.JPG",
            CreatedAt = LocalTimeService.LocalTime()
        };
        
        var gallery2 = new Gallery()
        {
            Id = Guid.NewGuid(),
            FileId = "be987e73-ccab-4bff-bc71-1781b2466f68",
            PathOfFile = "/Files/Photo_Gallery_2.JPG",
            CreatedAt = LocalTimeService.LocalTime()
        };
        
        var gallery3 = new Gallery()
        {
            Id = Guid.NewGuid(),
            FileId = "52512394-b580-425c-9eb4-5419628026da",
            PathOfFile = "/Files/Photo_Gallery_3.JPG",
            CreatedAt = LocalTimeService.LocalTime()
        };
        
        var gallery4 = new Gallery()
        {
            Id = Guid.NewGuid(),
            FileId = "4aa4f0c9-8ab2-46f9-9243-f7ab3426f238",
            PathOfFile = "/Files/Photo_Gallery_4.JPG",
            CreatedAt = LocalTimeService.LocalTime()
        };
        
        var gallery5 = new Gallery()
        {
            Id = Guid.NewGuid(),
            FileId = "9c9e677f-fc4f-4360-82af-bda4c35d4027",
            PathOfFile = "/Files/Photo_Gallery_5.JPG",
            CreatedAt = LocalTimeService.LocalTime()
        };
        
        var gallery6 = new Gallery()
        {
            Id = Guid.NewGuid(),
            FileId = "a9fdb3c2-0101-4f55-a3e5-96944620ab40",
            PathOfFile = "/Files/Photo_Gallery_6.JPG",
            CreatedAt = LocalTimeService.LocalTime()
        };
        
        var gallery7 = new Gallery()
        {
            Id = Guid.NewGuid(),
            FileId = "7bd09659-45be-4be3-bc2c-ae120e3d46f9",
            PathOfFile = "/Files/Photo_Gallery_7.JPG",
            CreatedAt = LocalTimeService.LocalTime()
        };
        
        var gallery8 = new Gallery()
        {
            Id = Guid.NewGuid(),
            FileId = "6492f5cb-ccac-48fc-b8da-7fc26ee5e723",
            PathOfFile = "/Files/Photo_Gallery_1.JPG",
            CreatedAt = LocalTimeService.LocalTime()
        };
        
        var gallery9 = new Gallery()
        {
            Id = Guid.NewGuid(),
            FileId = "b6814c38-5170-4b07-9929-4eccc8358374",
            PathOfFile = "/Files/Photo_Gallery_9.JPG",
            CreatedAt = LocalTimeService.LocalTime()
        };
        
        await _gallery.InsertManyAsync(new List<Gallery> { gallery1, gallery2, gallery3, gallery4, 
            gallery5, gallery6, gallery7, gallery8, gallery9 });
        
        Console.WriteLine("Database seeded with test data.");
    }
}