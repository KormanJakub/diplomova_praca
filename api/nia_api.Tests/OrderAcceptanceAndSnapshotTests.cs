using System.Net;
using System.Net.Http.Json;
using MongoDB.Bson;
using MongoDB.Driver;
using nia_api.Controllers;
using nia_api.Data;
using nia_api.Domain.Orders;
using nia_api.Enums;
using nia_api.Models;
using nia_api.Requests;
using nia_api.Security;
using nia_api.Services;

namespace nia_api.Tests;

public class OrderAcceptanceAndSnapshotTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public OrderAcceptanceAndSnapshotTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Parallel_Checkout_Last_Item_Allows_Only_One()
    {
        // Arrange
        var db = _factory.GetDbContext();
        var sequenceStore = new MongoOrderSequenceStore(db);
        var orderService = new OrderService(db, sequenceStore);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Limited Hoodie",
            Price = 40.00m,
            Colors = new List<Colors>
            {
                new()
                {
                    Name = "Black",
                    FileId = "f1",
                    PathOfFile = "limited-black.png",
                    Sizes = new List<SizeInfo>
                    {
                        new() { Size = "M", Quantity = 1 } // Only 1 in stock!
                    }
                }
            }
        };
        await db.Products.InsertOneAsync(product);

        var design = new Design
        {
            Id = Guid.NewGuid(),
            Name = "Dragon",
            Price = 10.00m,
            PathOfFile = "dragon.png"
        };
        await db.Designs.InsertOneAsync(design);

        var user1 = await _factory.SeedUserAsync(isAdmin: false);
        var user2 = await _factory.SeedUserAsync(isAdmin: false);

        var cust1 = new Customization
        {
            Id = Guid.NewGuid(),
            UserId = user1.Id.ToString(),
            ProductId = product.Id.ToString(),
            ProductColor = "Black",
            ProductSize = "M",
            DesignId = design.Id.ToString(),
            Price = 50.00m,
            IsOrdered = false
        };
        var cust2 = new Customization
        {
            Id = Guid.NewGuid(),
            UserId = user2.Id.ToString(),
            ProductId = product.Id.ToString(),
            ProductColor = "Black",
            ProductSize = "M",
            DesignId = design.Id.ToString(),
            Price = 50.00m,
            IsOrdered = false
        };
        await db.Customizations.InsertManyAsync(new[] { cust1, cust2 });

        // Act: Run both checkouts concurrently
        var task1 = orderService.CreateAsync(user1.Id, new[] { cust1.Id });
        var task2 = orderService.CreateAsync(user2.Id, new[] { cust2.Id });
        var results = await Task.WhenAll(task1, task2);

        // Assert
        var successes = results.Where(r => r.Order != null).ToList();
        var outOfStocks = results.Where(r => r.Error == OrderCreationError.OutOfStock).ToList();

        Assert.Single(successes);
        Assert.Single(outOfStocks);

        // Check product inventory in database is exactly 0, not negative
        var updatedProduct = await db.Products.Find(p => p.Id == product.Id).FirstOrDefaultAsync();
        var sizeInfo = updatedProduct.Colors.First(c => c.Name == "Black").Sizes.First(s => s.Size == "M");
        Assert.Equal(0, sizeInfo.Quantity);

        // Check that the failed customization remains unordered
        var failedCustId = successes[0].Order!.UserId == user1.Id ? cust2.Id : cust1.Id;
        var failedCust = await db.Customizations.Find(c => c.Id == failedCustId).FirstOrDefaultAsync();
        Assert.False(failedCust.IsOrdered);
    }

    [Fact]
    public async Task MultiItem_Checkout_Partial_Failure_Compensates_Stock_And_Claims()
    {
        // Arrange
        var db = _factory.GetDbContext();
        var sequenceStore = new MongoOrderSequenceStore(db);
        var orderService = new OrderService(db, sequenceStore);

        var productInStock = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Available Item",
            Price = 20.00m,
            Colors = new List<Colors>
            {
                new()
                {
                    Name = "Red",
                    FileId = "f1",
                    PathOfFile = "red.png",
                    Sizes = new List<SizeInfo>
                    {
                        new() { Size = "L", Quantity = 5 } // 5 available
                    }
                }
            }
        };
        var productOutOfStock = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Sold Out Item",
            Price = 25.00m,
            Colors = new List<Colors>
            {
                new()
                {
                    Name = "Blue",
                    FileId = "f2",
                    PathOfFile = "blue.png",
                    Sizes = new List<SizeInfo>
                    {
                        new() { Size = "S", Quantity = 0 } // 0 available!
                    }
                }
            }
        };
        await db.Products.InsertManyAsync(new[] { productInStock, productOutOfStock });

        var design = new Design { Id = Guid.NewGuid(), Name = "Skull", Price = 5.00m, PathOfFile = "skull.png" };
        await db.Designs.InsertOneAsync(design);

        var user = await _factory.SeedUserAsync(isAdmin: false);
        var cust1 = new Customization
        {
            Id = Guid.NewGuid(),
            UserId = user.Id.ToString(),
            ProductId = productInStock.Id.ToString(),
            ProductColor = "Red",
            ProductSize = "L",
            DesignId = design.Id.ToString(),
            Price = 25.00m,
            IsOrdered = false
        };
        var cust2 = new Customization
        {
            Id = Guid.NewGuid(),
            UserId = user.Id.ToString(),
            ProductId = productOutOfStock.Id.ToString(),
            ProductColor = "Blue",
            ProductSize = "S",
            DesignId = design.Id.ToString(),
            Price = 30.00m,
            IsOrdered = false
        };
        await db.Customizations.InsertManyAsync(new[] { cust1, cust2 });

        // Act: Attempt multi-item order where second item fails
        var result = await orderService.CreateAsync(user.Id, new[] { cust1.Id, cust2.Id });

        // Assert: Order creation failed with OutOfStock
        Assert.Null(result.Order);
        Assert.Equal(OrderCreationError.OutOfStock, result.Error);

        // Compensation check: stock of product 1 must be restored to original 5
        var p1 = await db.Products.Find(p => p.Id == productInStock.Id).FirstOrDefaultAsync();
        Assert.Equal(5, p1.Colors.First(c => c.Name == "Red").Sizes.First(s => s.Size == "L").Quantity);

        // Compensation check: customization 1 and 2 are released
        var c1 = await db.Customizations.Find(c => c.Id == cust1.Id).FirstOrDefaultAsync();
        var c2 = await db.Customizations.Find(c => c.Id == cust2.Id).FirstOrDefaultAsync();
        Assert.False(c1.IsOrdered);
        Assert.False(c2.IsOrdered);

        // No order was inserted
        var count = await db.Orders.CountDocumentsAsync(o => o.UserId == user.Id);
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Duplicate_Checkout_With_IdempotencyKey_Returns_Existing_Order()
    {
        // Arrange
        var db = _factory.GetDbContext();
        var sequenceStore = new MongoOrderSequenceStore(db);
        var orderService = new OrderService(db, sequenceStore);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Idempotent Shirt",
            Price = 15.00m,
            Colors = new List<Colors>
            {
                new()
                {
                    Name = "Green",
                    FileId = "f1",
                    PathOfFile = "green.png",
                    Sizes = new List<SizeInfo>
                    {
                        new() { Size = "S", Quantity = 10 }
                    }
                }
            }
        };
        await db.Products.InsertOneAsync(product);

        var design = new Design { Id = Guid.NewGuid(), Name = "Logo", Price = 5.00m, PathOfFile = "logo.png" };
        await db.Designs.InsertOneAsync(design);

        var user = await _factory.SeedUserAsync(isAdmin: false);
        var cust = new Customization
        {
            Id = Guid.NewGuid(),
            UserId = user.Id.ToString(),
            ProductId = product.Id.ToString(),
            ProductColor = "Green",
            ProductSize = "S",
            DesignId = design.Id.ToString(),
            Price = 20.00m,
            IsOrdered = false
        };
        await db.Customizations.InsertOneAsync(cust);

        var idempotencyKey = $"idemp-{Guid.NewGuid():N}";

        // Act 1: First checkout
        var firstResult = await orderService.CreateAsync(
            user.Id,
            new[] { cust.Id },
            idempotencyKey: idempotencyKey);

        Assert.NotNull(firstResult.Order);
        Assert.Null(firstResult.Error);
        var originalOrderId = firstResult.Order.Id;

        // Stock is now 9
        var pAfter1 = await db.Products.Find(p => p.Id == product.Id).FirstOrDefaultAsync();
        Assert.Equal(9, pAfter1.Colors.First(c => c.Name == "Green").Sizes.First(s => s.Size == "S").Quantity);

        // Act 2: Replay identical checkout with same Idempotency-Key
        var replayResult = await orderService.CreateAsync(
            user.Id,
            new[] { cust.Id },
            idempotencyKey: idempotencyKey);

        // Assert 2: Returns existing order without re-decrementing inventory
        Assert.NotNull(replayResult.Order);
        Assert.Equal(originalOrderId, replayResult.Order.Id);

        var pAfter2 = await db.Products.Find(p => p.Id == product.Id).FirstOrDefaultAsync();
        Assert.Equal(9, pAfter2.Colors.First(c => c.Name == "Green").Sizes.First(s => s.Size == "S").Quantity);
    }

    [Fact]
    public async Task Order_Numbering_Allocates_Unique_Sequential_Numbers()
    {
        // Arrange
        var db = _factory.GetDbContext();
        var sequenceStore = new MongoOrderSequenceStore(db);
        var orderService = new OrderService(db, sequenceStore);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Numbered T-Shirt",
            Price = 10.00m,
            Colors = new List<Colors>
            {
                new()
                {
                    Name = "White",
                    FileId = "f1",
                    PathOfFile = "white.png",
                    Sizes = new List<SizeInfo>
                    {
                        new() { Size = "XL", Quantity = 100 }
                    }
                }
            }
        };
        await db.Products.InsertOneAsync(product);

        var design = new Design { Id = Guid.NewGuid(), Name = "Art", Price = 5.00m, PathOfFile = "art.png" };
        await db.Designs.InsertOneAsync(design);

        var user = await _factory.SeedUserAsync(isAdmin: false);

        var orderIds = new List<int>();
        var orderNumbers = new List<string>();

        // Act: Create 3 sequential orders
        for (int i = 0; i < 3; i++)
        {
            var cust = new Customization
            {
                Id = Guid.NewGuid(),
                UserId = user.Id.ToString(),
                ProductId = product.Id.ToString(),
                ProductColor = "White",
                ProductSize = "XL",
                DesignId = design.Id.ToString(),
                Price = 15.00m,
                IsOrdered = false
            };
            await db.Customizations.InsertOneAsync(cust);

            var res = await orderService.CreateAsync(user.Id, new[] { cust.Id });
            Assert.NotNull(res.Order);
            Assert.NotNull(res.Order.OrderNumber);
            orderIds.Add(res.Order.Id);
            orderNumbers.Add(res.Order.OrderNumber!);
        }

        // Assert: Strictly distinct and sequential
        Assert.Equal(3, orderIds.Distinct().Count());
        Assert.True(orderIds[1] > orderIds[0]);
        Assert.True(orderIds[2] > orderIds[1]);

        Assert.Equal(3, orderNumbers.Distinct().Count());
        var currentYear = DateTime.UtcNow.Year;
        Assert.All(orderNumbers, num => Assert.StartsWith($"ORD-{currentYear}-", num));
    }

    [Fact]
    public async Task Historical_Order_Is_Immune_To_Catalog_Edits()
    {
        // Arrange
        var db = _factory.GetDbContext();
        var sequenceStore = new MongoOrderSequenceStore(db);
        var orderService = new OrderService(db, sequenceStore);

        var originalProductName = "Original Vintage Hoodie";
        var originalDesignName = "Original Sunset";
        var originalDesignPath = "sunset-v1.png";
        var originalProductPath = "hoodie-v1.png";

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = originalProductName,
            Price = 30.00m,
            Colors = new List<Colors>
            {
                new()
                {
                    Name = "VintageBlue",
                    FileId = "f1",
                    PathOfFile = originalProductPath,
                    Sizes = new List<SizeInfo>
                    {
                        new() { Size = "L", Quantity = 10 }
                    }
                }
            }
        };
        await db.Products.InsertOneAsync(product);

        var design = new Design
        {
            Id = Guid.NewGuid(),
            Name = originalDesignName,
            Price = 12.00m,
            PathOfFile = originalDesignPath
        };
        await db.Designs.InsertOneAsync(design);

        var customer = await _factory.SeedUserAsync(isAdmin: false);
        var cust = new Customization
        {
            Id = Guid.NewGuid(),
            UserId = customer.Id.ToString(),
            ProductId = product.Id.ToString(),
            ProductColor = "VintageBlue",
            ProductSize = "L",
            DesignId = design.Id.ToString(),
            UserDescription = "Keep it vintage",
            Price = 42.00m,
            IsOrdered = false
        };
        await db.Customizations.InsertOneAsync(cust);

        // Create the order with snapshots
        var creation = await orderService.CreateAsync(customer.Id, new[] { cust.Id });
        Assert.NotNull(creation.Order);
        var orderId = creation.Order.Id;

        // ACT: Mutate the catalog in database!
        // 1. Rename product and increase its price to 99.00
        await db.Products.UpdateOneAsync(
            p => p.Id == product.Id,
            Builders<Product>.Update
                .Set(p => p.Name, "Mutated New Hoodie")
                .Set(p => p.Price, 99.00m)
                .Set("colors.$[c].pathOfFile", "mutated-hoodie.png"),
            new UpdateOptions
            {
                ArrayFilters = new[] { new BsonDocumentArrayFilterDefinition<BsonDocument>(new BsonDocument("c.name", "VintageBlue")) }
            });

        // 2. Completely delete the design document from the database!
        await db.Designs.DeleteOneAsync(d => d.Id == design.Id);

        // 3. Query order via Customer endpoint
        var customerClient = _factory.CreateAuthenticatedClient(customer);
        var customerResponse = await customerClient.GetAsync($"/user/orders/{orderId}");
        Assert.Equal(HttpStatusCode.OK, customerResponse.StatusCode);

        var customerPayload = await customerResponse.Content.ReadFromJsonAsync<OrderDetailsResponse>();
        Assert.NotNull(customerPayload);
        Assert.NotNull(customerPayload.order);
        Assert.NotEmpty(customerPayload.products);
        Assert.NotEmpty(customerPayload.designs);

        // Assert: Product retains original name and original image path
        Assert.Equal(originalProductName, customerPayload.products[0].Name);
        Assert.Equal(originalProductPath, customerPayload.products[0].Colors[0].PathOfFile);

        // Assert: Design was completely deleted from DB, but order retains snapshot name and path!
        Assert.Equal(originalDesignName, customerPayload.designs[0].Name);
        Assert.Equal(originalDesignPath, customerPayload.designs[0].PathOfFile);

        // 4. Query order via Admin endpoint
        var admin = await _factory.SeedUserAsync(isAdmin: true);
        var adminClient = _factory.CreateAuthenticatedClient(admin);
        var adminResponse = await adminClient.GetAsync($"/admin/orders/{orderId}");
        Assert.Equal(HttpStatusCode.OK, adminResponse.StatusCode);

        var adminPayload = await adminResponse.Content.ReadFromJsonAsync<OrderDetailsResponse>();
        Assert.NotNull(adminPayload);
        Assert.Equal(originalProductName, adminPayload.products[0].Name);
        Assert.Equal(originalDesignName, adminPayload.designs[0].Name);
    }

    [Fact]
    public async Task Legacy_Order_Fallback_Works_Without_Snapshots()
    {
        // Arrange: Seed a legacy order that does NOT have Lines or CustomerSnapshot
        var db = _factory.GetDbContext();
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Legacy Product",
            Price = 18.00m,
            Colors = new List<Colors>
            {
                new()
                {
                    Name = "Yellow",
                    FileId = "f1",
                    PathOfFile = "yellow.png",
                    Sizes = new List<SizeInfo> { new() { Size = "M", Quantity = 2 } }
                }
            }
        };
        await db.Products.InsertOneAsync(product);

        var design = new Design { Id = Guid.NewGuid(), Name = "Legacy Design", Price = 4.00m, PathOfFile = "legacy.png" };
        await db.Designs.InsertOneAsync(design);

        var customer = await _factory.SeedUserAsync(isAdmin: false);
        var cust = new Customization
        {
            Id = Guid.NewGuid(),
            UserId = customer.Id.ToString(),
            ProductId = product.Id.ToString(),
            ProductColor = "Yellow",
            ProductSize = "M",
            DesignId = design.Id.ToString(),
            Price = 22.00m,
            IsOrdered = true
        };
        await db.Customizations.InsertOneAsync(cust);

        var legacyOrderId = new Random().Next(200000, 299999);
        var legacyOrder = new Order
        {
            Id = legacyOrderId,
            UserId = customer.Id,
            Customizations = new List<Guid> { cust.Id },
            TotalPrice = 22.00m,
            StatusOrder = EStatus.PRIJATA,
            Lines = null, // Legacy record without snapshot
            CustomerSnapshot = null,
            DeliverySnapshot = null,
            PricingSnapshot = null,
            CreatedAt = DateTime.UtcNow
        };
        await db.Orders.InsertOneAsync(legacyOrder);

        // Act: Fetch via Admin endpoint
        var admin = await _factory.SeedUserAsync(isAdmin: true);
        var adminClient = _factory.CreateAuthenticatedClient(admin);
        var response = await adminClient.GetAsync($"/admin/orders/{legacyOrderId}");

        // Assert: Successfully queried via legacy fallback
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<OrderDetailsResponse>();
        Assert.NotNull(payload);
        Assert.Equal("Legacy Product", payload.products[0].Name);
        Assert.Equal("Legacy Design", payload.designs[0].Name);
    }

    private sealed class OrderDetailsResponse
    {
        public Order? order { get; set; }
        public List<Customization> customizations { get; set; } = new();
        public List<Product> products { get; set; } = new();
        public List<Design> designs { get; set; } = new();
    }
}
