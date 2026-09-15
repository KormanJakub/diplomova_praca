using MongoDB.Driver;
using nia_api.Data;
using nia_api.Models;
using nia_api.Security;

namespace nia_api.Services;

public sealed class SecurityDataInitializer : IHostedService
{
    private readonly NiaDbContext _db;
    private readonly ILogger<SecurityDataInitializer> _logger;

    public SecurityDataInitializer(NiaDbContext db, ILogger<SecurityDataInitializer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var users = await _db.Users.Find(_ => true).ToListAsync(cancellationToken);
        foreach (var user in users.Where(u => !string.IsNullOrWhiteSpace(u.Email)))
        {
            var normalized = EmailNormalizer.Normalize(user.Email!);
            if (user.NormalizedEmail != normalized)
                await _db.Users.UpdateOneAsync(u => u.Id == user.Id,
                    Builders<User>.Update.Set(u => u.NormalizedEmail, normalized),
                    cancellationToken: cancellationToken);
        }

        var duplicates = users.Where(u => !string.IsNullOrWhiteSpace(u.Email))
            .GroupBy(u => EmailNormalizer.Normalize(u.Email!))
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();
        if (duplicates.Count > 0)
            throw new InvalidOperationException(
                "Duplicate normalized user emails must be resolved before the API can start securely.");

        await _db.Users.Indexes.CreateOneAsync(new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Ascending(u => u.NormalizedEmail),
            new CreateIndexOptions { Unique = true, Sparse = true, Name = "ux_users_normalized_email" }),
            cancellationToken: cancellationToken);

        var orders = await _db.Orders.Find(_ => true).ToListAsync(cancellationToken);
        foreach (var order in orders)
        {
            var update = new List<UpdateDefinition<Order>>();
            if (!string.IsNullOrWhiteSpace(order.CancellationToken) && !CapabilityToken.IsHash(order.CancellationToken))
                update.Add(Builders<Order>.Update.Set(o => o.CancellationToken, CapabilityToken.Hash(order.CancellationToken)));
            if (!string.IsNullOrWhiteSpace(order.FollowToken) && !CapabilityToken.IsHash(order.FollowToken))
                update.Add(Builders<Order>.Update.Set(o => o.FollowToken, CapabilityToken.Hash(order.FollowToken)));
            if (order.CancellationTokenExpiresAt == null)
                update.Add(Builders<Order>.Update.Set(o => o.CancellationTokenExpiresAt,
                    (order.CreatedAt ?? DateTime.UtcNow).AddHours(24)));
            if (order.FollowTokenExpiresAt == null)
                update.Add(Builders<Order>.Update.Set(o => o.FollowTokenExpiresAt,
                    (order.CreatedAt ?? DateTime.UtcNow).AddDays(180)));
            if (update.Count > 0)
                await _db.Orders.UpdateOneAsync(o => o.Id == order.Id,
                    Builders<Order>.Update.Combine(update), cancellationToken: cancellationToken);
        }

        var orderedCustomizationIds = orders
            .Where(o => o.StatusOrder != nia_api.Enums.EStatus.ZRUSENA)
            .SelectMany(o => o.Customizations ?? []).Distinct().ToList();
        if (orderedCustomizationIds.Count > 0)
            await _db.Customizations.UpdateManyAsync(c => orderedCustomizationIds.Contains(c.Id),
                Builders<Customization>.Update.Set(c => c.IsOrdered, true), cancellationToken: cancellationToken);

        await _db.Orders.Indexes.CreateManyAsync([
            new CreateIndexModel<Order>(Builders<Order>.IndexKeys.Ascending(o => o.CancellationToken),
                new CreateIndexOptions { Unique = true, Sparse = true, Name = "ux_orders_cancellation_token" }),
            new CreateIndexModel<Order>(Builders<Order>.IndexKeys.Ascending(o => o.FollowToken),
                new CreateIndexOptions { Unique = true, Sparse = true, Name = "ux_orders_follow_token" })
        ], cancellationToken);

        _logger.LogInformation("Security data migration and unique indexes completed.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
