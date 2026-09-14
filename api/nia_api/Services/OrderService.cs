using MongoDB.Bson;
using MongoDB.Driver;
using nia_api.Data;
using nia_api.Enums;
using nia_api.Models;

namespace nia_api.Services;

public class OrderService
{
    private readonly IMongoCollection<Customization> _customizations;
    private readonly IMongoCollection<Order> _orders;
    private readonly IMongoCollection<Product> _products;
    private readonly IMongoCollection<StoreSettings> _storeSettings;

    public OrderService(NiaDbContext context)
    {
        _customizations = context.Customizations;
        _orders = context.Orders;
        _products = context.Products;
        _storeSettings = context.StoreSettings;
    }

    public async Task<OrderCreationResult> CreateAsync(
        Guid userId,
        IReadOnlyList<Guid>? customizationIds,
        string? paymentMethod = "Stripe",
        string? deliveryMethod = "HomeDelivery",
        string? packetaPointId = null,
        string? packetaPointName = null,
        string? packetaPointAddress = null)
    {
        if (customizationIds == null || customizationIds.Count == 0 ||
            customizationIds.Count != customizationIds.Distinct().Count())
            return new(null, OrderCreationError.InvalidItems);

        var items = await _customizations.Find(c => customizationIds.Contains(c.Id)).ToListAsync();
        if (items.Count != customizationIds.Count || items.Any(c => c.UserId != userId.ToString()))
            return new(null, OrderCreationError.InvalidItems);

        var total = items.Sum(c => c.Price);
        if (total <= 0) return new(null, OrderCreationError.InvalidItems);

        decimal paymentFee = 0.00m;
        var canonicalPaymentMethod = paymentMethod ?? "Stripe";
        if (string.Equals(canonicalPaymentMethod, "dobierka", StringComparison.OrdinalIgnoreCase))
        {
            canonicalPaymentMethod = "Dobierka";
            var settings = await _storeSettings.Find(s => s.Id == "store_settings").FirstOrDefaultAsync();
            paymentFee = settings?.CashOnDeliveryFee ?? 1.00m;
        }
        else if (string.Equals(canonicalPaymentMethod, "iban", StringComparison.OrdinalIgnoreCase))
        {
            canonicalPaymentMethod = "IBAN";
        }
        else if (string.Equals(canonicalPaymentMethod, "stripe", StringComparison.OrdinalIgnoreCase))
        {
            canonicalPaymentMethod = "Stripe";
        }

        var decrementedItems = new List<(Guid ProductId, string Color, string Size)>();
        foreach (var item in items)
        {
            if (Guid.TryParse(item.ProductId, out var prodId) &&
                !string.IsNullOrWhiteSpace(item.ProductColor) &&
                !string.IsNullOrWhiteSpace(item.ProductSize))
            {
                var success = await DecrementStockAsync(prodId, item.ProductColor, item.ProductSize);
                if (!success)
                {
                    foreach (var dec in decrementedItems)
                    {
                        await IncrementStockAsync(dec.ProductId, dec.Color, dec.Size);
                    }
                    return new(null, OrderCreationError.OutOfStock);
                }
                decrementedItems.Add((prodId, item.ProductColor, item.ProductSize));
            }
        }

        var last = await _orders.Find(FilterDefinition<Order>.Empty)
            .SortByDescending(o => o.Id).FirstOrDefaultAsync();
        var order = new Order
        {
            Id = (last?.Id ?? 0) + 1,
            Customizations = customizationIds.ToList(),
            TotalPrice = total + paymentFee,
            UserId = userId,
            StatusOrder = EStatus.PRIJATA,
            PaymentMethod = canonicalPaymentMethod,
            PaymentFee = paymentFee,
            DeliveryMethod = string.IsNullOrWhiteSpace(deliveryMethod) ? "HomeDelivery" : deliveryMethod,
            PacketaPointId = packetaPointId,
            PacketaPointName = packetaPointName,
            PacketaPointAddress = packetaPointAddress,
            CancellationToken = Guid.NewGuid().ToString("N"),
            FollowToken = Guid.NewGuid().ToString("N"),
            PaymentStatus = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        for (var attempt = 0; attempt < 10; attempt++)
        {
            try
            {
                await _orders.InsertOneAsync(order);
                return new(order, null);
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                last = await _orders.Find(FilterDefinition<Order>.Empty)
                    .SortByDescending(o => o.Id).FirstOrDefaultAsync();
                order.Id = (last?.Id ?? 0) + 1;
            }
        }

        foreach (var dec in decrementedItems)
        {
            await IncrementStockAsync(dec.ProductId, dec.Color, dec.Size);
        }

        return new(null, OrderCreationError.NumberConflict);
    }

    public async Task<bool> CancelAsync(FilterDefinition<Order> filter)
    {
        var order = await _orders.Find(filter).FirstOrDefaultAsync();
        if (order == null) return false;

        var cancelFilter = Builders<Order>.Filter.And(
            filter,
            Builders<Order>.Filter.Ne(o => o.StatusOrder, EStatus.ZRUSENA)
        );
        var update = Builders<Order>.Update
            .Set(o => o.StatusOrder, EStatus.ZRUSENA)
            .Set(o => o.UpdatedAt, DateTime.UtcNow);

        var result = await _orders.UpdateOneAsync(cancelFilter, update);
        if (result.ModifiedCount == 0) return false;

        if (order.Customizations != null && order.Customizations.Count > 0)
        {
            var customizations = await _customizations
                .Find(c => order.Customizations.Contains(c.Id))
                .ToListAsync();

            foreach (var cust in customizations)
            {
                if (Guid.TryParse(cust.ProductId, out var prodId) &&
                    !string.IsNullOrWhiteSpace(cust.ProductColor) &&
                    !string.IsNullOrWhiteSpace(cust.ProductSize))
                {
                    await IncrementStockAsync(prodId, cust.ProductColor, cust.ProductSize);
                }
            }
        }

        return true;
    }

    public async Task<bool> CancelByTokenAsync(string cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(cancellationToken)) return false;
        var filter = Builders<Order>.Filter.And(
            Builders<Order>.Filter.Eq(o => o.CancellationToken, cancellationToken),
            Builders<Order>.Filter.Eq(o => o.StatusOrder, EStatus.PRIJATA)
        );
        return await CancelAsync(filter);
    }

    public async Task<bool> CancelByUserAsync(int orderId, Guid userId)
    {
        var filter = Builders<Order>.Filter.And(
            Builders<Order>.Filter.Eq(o => o.Id, orderId),
            Builders<Order>.Filter.Eq(o => o.UserId, userId),
            Builders<Order>.Filter.Eq(o => o.StatusOrder, EStatus.PRIJATA)
        );
        return await CancelAsync(filter);
    }

    public async Task<bool> CancelByAdminAsync(int orderId)
    {
        var filter = Builders<Order>.Filter.Eq(o => o.Id, orderId);
        return await CancelAsync(filter);
    }

    private async Task<bool> DecrementStockAsync(Guid productId, string colorName, string sizeName)
    {
        var filter = Builders<Product>.Filter.And(
            Builders<Product>.Filter.Eq(p => p.Id, productId),
            Builders<Product>.Filter.ElemMatch(p => p.Colors, c =>
                c.Name == colorName && c.Sizes.Any(s => s.Size == sizeName && s.Quantity > 0))
        );
        var update = Builders<Product>.Update.Inc("colors.$[color].sizes.$[size].quantity", -1);
        var arrayFilters = new[]
        {
            new BsonDocumentArrayFilterDefinition<BsonDocument>(new BsonDocument("color.name", colorName)),
            new BsonDocumentArrayFilterDefinition<BsonDocument>(new BsonDocument("size.size", sizeName))
        };
        var options = new UpdateOptions { ArrayFilters = arrayFilters };
        var result = await _products.UpdateOneAsync(filter, update, options);
        return result.ModifiedCount > 0;
    }

    private async Task IncrementStockAsync(Guid productId, string colorName, string sizeName)
    {
        var filter = Builders<Product>.Filter.And(
            Builders<Product>.Filter.Eq(p => p.Id, productId),
            Builders<Product>.Filter.ElemMatch(p => p.Colors, c =>
                c.Name == colorName && c.Sizes.Any(s => s.Size == sizeName))
        );
        var update = Builders<Product>.Update.Inc("colors.$[color].sizes.$[size].quantity", 1);
        var arrayFilters = new[]
        {
            new BsonDocumentArrayFilterDefinition<BsonDocument>(new BsonDocument("color.name", colorName)),
            new BsonDocumentArrayFilterDefinition<BsonDocument>(new BsonDocument("size.size", sizeName))
        };
        var options = new UpdateOptions { ArrayFilters = arrayFilters };
        await _products.UpdateOneAsync(filter, update, options);
    }
}

public enum OrderCreationError { InvalidItems, OutOfStock, NumberConflict }
public sealed record OrderCreationResult(Order? Order, OrderCreationError? Error);
