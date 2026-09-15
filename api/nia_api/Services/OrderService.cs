using MongoDB.Bson;
using MongoDB.Driver;
using nia_api.Data;
using nia_api.Domain.Configuration;
using nia_api.Domain.Orders;
using nia_api.Enums;
using nia_api.Models;
using nia_api.Security;

namespace nia_api.Services;

public class OrderService
{
    private readonly IMongoCollection<Customization> _customizations;
    private readonly IMongoCollection<Order> _orders;
    private readonly IMongoCollection<Product> _products;
    private readonly IMongoCollection<Design> _designs;
    private readonly IMongoCollection<User> _users;
    private readonly IMongoCollection<GuestUser> _guestUsers;
    private readonly IMongoCollection<StoreSettings> _storeSettings;
    private readonly IOrderSequenceStore _sequenceStore;
    private readonly IMerchantConfigurationService _configService;

    public OrderService(NiaDbContext context, IOrderSequenceStore? sequenceStore = null, IMerchantConfigurationService? configService = null)
    {
        _customizations = context.Customizations;
        _orders = context.Orders;
        _products = context.Products;
        _designs = context.Designs;
        _users = context.Users;
        _guestUsers = context.GuestUsers;
        _storeSettings = context.StoreSettings;
        _sequenceStore = sequenceStore ?? new MongoOrderSequenceStore(context);
        _configService = configService ?? new MerchantConfigurationService(context);
    }

    public async Task<OrderCreationResult> CreateAsync(
        Guid userId,
        IReadOnlyList<Guid>? customizationIds,
        string? paymentMethod = "Stripe",
        string? deliveryMethod = "HomeDelivery",
        string? packetaPointId = null,
        string? packetaPointName = null,
        string? packetaPointAddress = null,
        string? idempotencyKey = null)
    {
        if (customizationIds == null || customizationIds.Count == 0 || customizationIds.Count > 25 ||
            customizationIds.Count != customizationIds.Distinct().Count())
            return new(null, OrderCreationError.InvalidItems);
        if ((packetaPointId?.Length ?? 0) > 100 || (packetaPointName?.Length ?? 0) > 200 ||
            (packetaPointAddress?.Length ?? 0) > 300)
            return new(null, OrderCreationError.InvalidItems);

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var existingOrder = await _orders.Find(o => o.IdempotencyKey == idempotencyKey && o.UserId == userId).FirstOrDefaultAsync();
            if (existingOrder != null)
            {
                return new(existingOrder, null);
            }
        }

        var items = await _customizations.Find(c => customizationIds.Contains(c.Id)).ToListAsync();
        if (items.Count != customizationIds.Count || items.Any(c =>
                c.UserId != userId.ToString() || c.IsOrdered ||
                !Guid.TryParse(c.ProductId, out _) ||
                string.IsNullOrWhiteSpace(c.ProductColor) ||
                string.IsNullOrWhiteSpace(c.ProductSize)))
            return new(null, OrderCreationError.InvalidItems);

        var total = items.Sum(c => c.Price);
        if (total <= 0) return new(null, OrderCreationError.InvalidItems);

        var canonicalPaymentMethod = paymentMethod ?? "Stripe";
        if (string.Equals(canonicalPaymentMethod, "dobierka", StringComparison.OrdinalIgnoreCase))
        {
            canonicalPaymentMethod = "Dobierka";
        }
        else if (string.Equals(canonicalPaymentMethod, "iban", StringComparison.OrdinalIgnoreCase))
        {
            canonicalPaymentMethod = "IBAN";
        }
        else if (string.Equals(canonicalPaymentMethod, "stripe", StringComparison.OrdinalIgnoreCase))
        {
            canonicalPaymentMethod = "Stripe";
        }
        else
        {
            return new(null, OrderCreationError.InvalidItems);
        }

        var payValidation = await _configService.ValidatePaymentMethodAllowedAsync(canonicalPaymentMethod);
        if (!payValidation.IsAllowed)
            return new(null, OrderCreationError.InvalidItems);
        decimal paymentFee = payValidation.Fee;

        var canonicalDeliveryMethod = deliveryMethod ?? "HomeDelivery";
        if (string.Equals(canonicalDeliveryMethod, "homedelivery", StringComparison.OrdinalIgnoreCase))
            canonicalDeliveryMethod = "HomeDelivery";
        else if (string.Equals(canonicalDeliveryMethod, "packeta", StringComparison.OrdinalIgnoreCase))
            canonicalDeliveryMethod = "Packeta";
        else
            return new(null, OrderCreationError.InvalidItems);

        var delivValidation = await _configService.ValidateDeliveryMethodAllowedAsync(canonicalDeliveryMethod);
        if (!delivValidation.IsAllowed)
            return new(null, OrderCreationError.InvalidItems);
        decimal deliveryFee = delivValidation.Fee;

        if (canonicalDeliveryMethod == "Packeta" &&
            (string.IsNullOrWhiteSpace(packetaPointId) || string.IsNullOrWhiteSpace(packetaPointName) ||
             string.IsNullOrWhiteSpace(packetaPointAddress)))
            return new(null, OrderCreationError.InvalidItems);

        var isPersonalizationEnabled = await _configService.IsPersonalizationEnabledAsync();
        if (!isPersonalizationEnabled)
        {
            if (items.Any(c => !string.IsNullOrEmpty(c.DesignId) || !string.IsNullOrWhiteSpace(c.UserDescription)))
                return new(null, OrderCreationError.InvalidItems);
        }

        var claimedItems = new List<Guid>();
        foreach (var item in items)
        {
            var claimFilter = Builders<Customization>.Filter.And(
                Builders<Customization>.Filter.Eq(c => c.Id, item.Id),
                Builders<Customization>.Filter.Eq(c => c.UserId, userId.ToString()),
                Builders<Customization>.Filter.Ne(c => c.IsOrdered, true));
            var claim = await _customizations.UpdateOneAsync(claimFilter,
                Builders<Customization>.Update.Set(c => c.IsOrdered, true));
            if (claim.ModifiedCount == 0)
            {
                await ReleaseCustomizationsAsync(claimedItems);
                return new(null, OrderCreationError.InvalidItems);
            }
            claimedItems.Add(item.Id);
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
                    await ReleaseCustomizationsAsync(claimedItems);
                    return new(null, OrderCreationError.OutOfStock);
                }
                decrementedItems.Add((prodId, item.ProductColor, item.ProductSize));
            }
        }

        var productGuids = items
            .Select(c => Guid.TryParse(c.ProductId, out var pid) ? pid : Guid.Empty)
            .Where(pid => pid != Guid.Empty)
            .Distinct()
            .ToList();
        var products = await _products.Find(p => productGuids.Contains(p.Id)).ToListAsync();

        var designGuids = items
            .Select(c => Guid.TryParse(c.DesignId, out var did) ? did : Guid.Empty)
            .Where(did => did != Guid.Empty)
            .Distinct()
            .ToList();
        var designs = await _designs.Find(d => designGuids.Contains(d.Id)).ToListAsync();

        var lines = items.Select(item =>
        {
            Guid.TryParse(item.ProductId, out var pid);
            Guid.TryParse(item.DesignId, out var did);
            var prod = products.FirstOrDefault(p => p.Id == pid);
            var des = designs.FirstOrDefault(d => d.Id == did);
            var colorObj = prod?.Colors?.FirstOrDefault(c => string.Equals(c.Name, item.ProductColor, StringComparison.OrdinalIgnoreCase));

            return new OrderLineSnapshot
            {
                CustomizationId = item.Id,
                ProductId = pid,
                ProductName = prod?.Name ?? "Neznámy produkt",
                ProductDescription = prod?.Description,
                ProductColor = item.ProductColor ?? "",
                ProductSize = item.ProductSize ?? "",
                ProductImagePath = colorObj?.PathOfFile ?? "",
                ProductPrice = prod?.Price ?? 0.00m,
                DesignId = did,
                DesignName = des?.Name ?? "Neznámy dizajn",
                DesignPrice = des?.Price ?? 0.00m,
                DesignImagePath = des?.PathOfFile ?? "",
                CustomizationDescription = item.UserDescription,
                UnitPrice = item.Price,
                Quantity = 1,
                LineTotal = item.Price
            };
        }).ToList();

        OrderCustomerSnapshot customerSnapshot;
        var user = await _users.Find(u => u.Id == userId).FirstOrDefaultAsync();
        if (user != null)
        {
            customerSnapshot = new OrderCustomerSnapshot
            {
                UserId = userId,
                FirstName = user.FirstName ?? "",
                LastName = user.LastName ?? "",
                Email = user.Email ?? "",
                PhoneNumber = user.PhoneNumber ?? "",
                Address = user.Address ?? "",
                Zip = user.Zip ?? "",
                Country = user.Country ?? "",
                IsGuest = false
            };
        }
        else
        {
            var guest = await _guestUsers.Find(g => g.Id == userId).FirstOrDefaultAsync();
            customerSnapshot = new OrderCustomerSnapshot
            {
                UserId = userId,
                FirstName = guest?.FirstName ?? "",
                LastName = guest?.LastName ?? "",
                Email = guest?.Email ?? "",
                PhoneNumber = guest?.PhoneNumber ?? "",
                Address = guest?.Address ?? "",
                Zip = guest?.Zip ?? "",
                Country = guest?.Country ?? "",
                IsGuest = true
            };
        }

        var deliverySnapshot = new OrderDeliverySnapshot
        {
            DeliveryMethod = canonicalDeliveryMethod,
            PacketaPointId = packetaPointId,
            PacketaPointName = packetaPointName,
            PacketaPointAddress = packetaPointAddress,
            DeliveryFee = deliveryFee
        };

        var pricingSnapshot = new OrderPricingSnapshot
        {
            Currency = "EUR",
            ItemsSubtotal = total,
            PaymentFee = paymentFee,
            DeliveryFee = deliveryFee,
            TotalPrice = total + paymentFee + deliveryFee,
            TaxRate = 0.20m,
            TaxAmount = decimal.Round((total + paymentFee + deliveryFee) * 0.20m / 1.20m, 2)
        };

        var cancellationToken = CapabilityToken.Create();
        var followToken = CapabilityToken.Create();

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var orderId = await _sequenceStore.NextOrderIdAsync();
            var orderNumber = await _sequenceStore.NextOrderNumberAsync();

            var order = new Order
            {
                Id = orderId,
                OrderNumber = orderNumber,
                Customizations = customizationIds.ToList(),
                TotalPrice = total + paymentFee + deliveryFee,
                UserId = userId,
                StatusOrder = EStatus.PRIJATA,
                PaymentMethod = canonicalPaymentMethod,
                PaymentFee = paymentFee,
                DeliveryMethod = canonicalDeliveryMethod,
                DeliveryFee = deliveryFee,
                PacketaPointId = packetaPointId,
                PacketaPointName = packetaPointName,
                PacketaPointAddress = packetaPointAddress,
                Lines = lines,
                CustomerSnapshot = customerSnapshot,
                DeliverySnapshot = deliverySnapshot,
                PricingSnapshot = pricingSnapshot,
                IdempotencyKey = idempotencyKey,
                CancellationToken = CapabilityToken.Hash(cancellationToken),
                CancellationTokenExpiresAt = DateTime.UtcNow.AddHours(24),
                FollowToken = CapabilityToken.Hash(followToken),
                FollowTokenExpiresAt = DateTime.UtcNow.AddDays(180),
                PaymentStatus = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                await _orders.InsertOneAsync(order);
                return new(order, null, cancellationToken, followToken);
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                if (!string.IsNullOrWhiteSpace(idempotencyKey))
                {
                    var existing = await _orders.Find(o => o.IdempotencyKey == idempotencyKey && o.UserId == userId).FirstOrDefaultAsync();
                    if (existing != null)
                    {
                        foreach (var dec in decrementedItems)
                        {
                            await IncrementStockAsync(dec.ProductId, dec.Color, dec.Size);
                        }
                        await ReleaseCustomizationsAsync(claimedItems);
                        return new(existing, null);
                    }
                }
                continue;
            }
            catch
            {
                foreach (var dec in decrementedItems)
                {
                    await IncrementStockAsync(dec.ProductId, dec.Color, dec.Size);
                }
                await ReleaseCustomizationsAsync(claimedItems);
                throw;
            }
        }

        foreach (var dec in decrementedItems)
        {
            await IncrementStockAsync(dec.ProductId, dec.Color, dec.Size);
        }
        await ReleaseCustomizationsAsync(claimedItems);

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
            await ReleaseCustomizationsAsync(order.Customizations);
        }

        return true;
    }

    public async Task<bool> CancelByTokenAsync(string cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(cancellationToken)) return false;
        var filter = Builders<Order>.Filter.And(
            Builders<Order>.Filter.Eq(o => o.CancellationToken, CapabilityToken.Hash(cancellationToken)),
            Builders<Order>.Filter.Gt(o => o.CancellationTokenExpiresAt, DateTime.UtcNow),
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

    private Task ReleaseCustomizationsAsync(IEnumerable<Guid> ids)
    {
        var idList = ids.Distinct().ToList();
        return idList.Count == 0
            ? Task.CompletedTask
            : _customizations.UpdateManyAsync(c => idList.Contains(c.Id),
                Builders<Customization>.Update.Set(c => c.IsOrdered, false));
    }
}

public enum OrderCreationError { InvalidItems, OutOfStock, NumberConflict }
public sealed record OrderCreationResult(
    Order? Order,
    OrderCreationError? Error,
    string? CancellationToken = null,
    string? FollowToken = null);
