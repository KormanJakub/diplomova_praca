using MongoDB.Driver;
using nia_api.Data;
using nia_api.Enums;
using nia_api.Models;

namespace nia_api.Services;

public sealed class OrderService
{
    private readonly IMongoCollection<Customization> _customizations;
    private readonly IMongoCollection<Order> _orders;

    public OrderService(NiaDbContext context)
    {
        _customizations = context.Customizations;
        _orders = context.Orders;
    }

    public async Task<OrderCreationResult> CreateAsync(Guid userId, IReadOnlyList<Guid>? customizationIds)
    {
        if (customizationIds == null || customizationIds.Count == 0 ||
            customizationIds.Count != customizationIds.Distinct().Count())
            return new(null, OrderCreationError.InvalidItems);

        var items = await _customizations.Find(c => customizationIds.Contains(c.Id)).ToListAsync();
        if (items.Count != customizationIds.Count || items.Any(c => c.UserId != userId.ToString()))
            return new(null, OrderCreationError.InvalidItems);

        var total = items.Sum(c => c.Price);
        if (total <= 0) return new(null, OrderCreationError.InvalidItems);

        var last = await _orders.Find(FilterDefinition<Order>.Empty)
            .SortByDescending(o => o.Id).FirstOrDefaultAsync();
        var order = new Order
        {
            Id = (last?.Id ?? 0) + 1,
            Customizations = customizationIds.ToList(),
            TotalPrice = total,
            UserId = userId,
            StatusOrder = EStatus.PRIJATA,
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

        return new(null, OrderCreationError.NumberConflict);
    }
}

public enum OrderCreationError { InvalidItems, NumberConflict }
public sealed record OrderCreationResult(Order? Order, OrderCreationError? Error);
