using MongoDB.Bson;
using MongoDB.Driver;
using nia_api.Data;
using nia_api.Models;

namespace nia_api.Domain.Orders;

public class MongoOrderSequenceStore : IOrderSequenceStore
{
    private readonly IMongoCollection<BsonDocument> _counters;
    private readonly IMongoCollection<Order> _orders;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    public MongoOrderSequenceStore(NiaDbContext dbContext)
    {
        _counters = dbContext.Counters;
        _orders = dbContext.Orders;
    }

    public async Task<int> NextOrderIdAsync(CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct);

        var filter = Builders<BsonDocument>.Filter.Eq("_id", "order_id");
        var update = Builders<BsonDocument>.Update.Inc("seq", 1);
        var options = new FindOneAndUpdateOptions<BsonDocument>
        {
            IsUpsert = true,
            ReturnDocument = ReturnDocument.After
        };

        var result = await _counters.FindOneAndUpdateAsync(filter, update, options, ct);
        return result["seq"].AsInt32;
    }

    public async Task<string> NextOrderNumberAsync(int? year = null, CancellationToken ct = default)
    {
        var targetYear = year ?? DateTime.UtcNow.Year;
        var counterKey = $"order_number_{targetYear}";

        var filter = Builders<BsonDocument>.Filter.Eq("_id", counterKey);
        var update = Builders<BsonDocument>.Update.Inc("seq", 1);
        var options = new FindOneAndUpdateOptions<BsonDocument>
        {
            IsUpsert = true,
            ReturnDocument = ReturnDocument.After
        };

        var result = await _counters.FindOneAndUpdateAsync(filter, update, options, ct);
        var seq = result["seq"].AsInt32;
        return $"ORD-{targetYear}-{seq:D5}";
    }

    private async Task EnsureInitializedAsync(CancellationToken ct)
    {
        if (_initialized) return;

        await _initLock.WaitAsync(ct);
        try
        {
            if (_initialized) return;

            var existing = await _counters.Find(Builders<BsonDocument>.Filter.Eq("_id", "order_id")).FirstOrDefaultAsync(ct);
            if (existing == null)
            {
                var highestOrder = await _orders.Find(FilterDefinition<Order>.Empty)
                    .SortByDescending(o => o.Id)
                    .FirstOrDefaultAsync(ct);

                var baseId = Math.Max(highestOrder?.Id ?? 100000, 100000);
                var doc = new BsonDocument
                {
                    { "_id", "order_id" },
                    { "seq", baseId }
                };

                try
                {
                    await _counters.InsertOneAsync(doc, cancellationToken: ct);
                }
                catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
                {
                    // Another instance initialized concurrently
                }
            }

            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }
}
