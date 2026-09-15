using MongoDB.Bson;
using MongoDB.Driver;
using nia_api.Data;
using nia_api.Enums;
using nia_api.Models;

namespace nia_api.Domain.Orders;

public class MongoOrderStore : IOrderStore
{
    private readonly IMongoCollection<Order> _orders;

    public MongoOrderStore(NiaDbContext dbContext)
    {
        _orders = dbContext.Orders;
    }

    public Task<Order?> GetByIdAsync(int orderId, CancellationToken ct = default)
    {
        return _orders.Find(o => o.Id == orderId).FirstOrDefaultAsync(ct)!;
    }

    public Task<Order?> GetByCancellationTokenHashAsync(string tokenHash, CancellationToken ct = default)
    {
        return _orders.Find(o => o.CancellationToken == tokenHash).FirstOrDefaultAsync(ct)!;
    }

    public Task<Order?> GetByFollowTokenHashAsync(string tokenHash, CancellationToken ct = default)
    {
        return _orders.Find(o => o.FollowToken == tokenHash).FirstOrDefaultAsync(ct)!;
    }

    public Task<List<Order>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return _orders.Find(o => o.UserId == userId).ToListAsync(ct);
    }

    public Task<List<Order>> GetAllAsync(CancellationToken ct = default)
    {
        return _orders.Find(_ => true).ToListAsync(ct);
    }

    public async Task<bool> UpdateStatusConditionalAsync(
        int orderId,
        EStatus expectedCurrentStatus,
        EStatus targetStatus,
        DateTime updatedAt,
        CancellationToken ct = default)
    {
        var filter = Builders<Order>.Filter.And(
            Builders<Order>.Filter.Eq(o => o.Id, orderId),
            Builders<Order>.Filter.Eq(o => o.StatusOrder, expectedCurrentStatus)
        );

        var update = Builders<Order>.Update
            .Set(o => o.StatusOrder, targetStatus)
            .Set(o => o.UpdatedAt, updatedAt);

        var result = await _orders.UpdateOneAsync(filter, update, cancellationToken: ct);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> UpdateDeliveryDetailsAsync(
        int orderId,
        string deliveryMethod,
        string? pointId,
        string? pointName,
        string? pointAddress,
        DateTime updatedAt,
        CancellationToken ct = default)
    {
        var filter = Builders<Order>.Filter.Eq(o => o.Id, orderId);
        var update = Builders<Order>.Update
            .Set(o => o.DeliveryMethod, deliveryMethod)
            .Set(o => o.PacketaPointId, pointId)
            .Set(o => o.PacketaPointName, pointName)
            .Set(o => o.PacketaPointAddress, pointAddress)
            .Set(o => o.UpdatedAt, updatedAt);

        var result = await _orders.UpdateOneAsync(filter, update, cancellationToken: ct);
        return result.MatchedCount > 0;
    }

    public async Task<bool> MarkPaidConditionalAsync(
        int orderId,
        EStatus newStatus,
        DateTime updatedAt,
        CancellationToken ct = default)
    {
        var filter = Builders<Order>.Filter.And(
            Builders<Order>.Filter.Eq(o => o.Id, orderId),
            Builders<Order>.Filter.Ne(o => o.StatusOrder, EStatus.ZRUSENA)
        );

        var update = Builders<Order>.Update
            .Set(o => o.PaymentStatus, "Paid")
            .Set(o => o.StatusOrder, newStatus)
            .Set(o => o.UpdatedAt, updatedAt);

        var result = await _orders.UpdateOneAsync(filter, update, cancellationToken: ct);
        return result.ModifiedCount > 0;
    }

    public async Task<SalesSummaryDto> GetSalesSummaryAsync(CancellationToken ct = default)
    {
        var soldCount = await _orders.CountDocumentsAsync(o => o.StatusOrder == EStatus.ZAPLATENA, cancellationToken: ct);
        var pendingCount = await _orders.CountDocumentsAsync(o => o.StatusOrder == EStatus.PRIJATA, cancellationToken: ct);
        var makingCount = await _orders.CountDocumentsAsync(o => o.StatusOrder == EStatus.VO_VYROBE, cancellationToken: ct);
        var readyCount = await _orders.CountDocumentsAsync(o => o.StatusOrder == EStatus.PRIPRAVENA, cancellationToken: ct);
        var sendCount = await _orders.CountDocumentsAsync(o => o.StatusOrder == EStatus.POSLANA, cancellationToken: ct);
        var cancelCount = await _orders.CountDocumentsAsync(o => o.StatusOrder == EStatus.ZRUSENA, cancellationToken: ct);

        return new SalesSummaryDto(
            SoldOrders: soldCount,
            PendingOrders: pendingCount,
            MakingOrders: makingCount,
            ReadyOrders: readyCount,
            SendOrders: sendCount,
            CancelOrders: cancelCount);
    }

    public async Task<KpiDataDto> GetKpiDataAsync(CancellationToken ct = default)
    {
        var filter = Builders<Order>.Filter.Ne(o => o.StatusOrder, EStatus.ZRUSENA);
        var paidFilter = Builders<Order>.Filter.Eq(o => o.PaymentStatus, "Paid");

        var totalOrders = await _orders.CountDocumentsAsync(filter, cancellationToken: ct);

        var revenueAggregate = await _orders.Aggregate()
            .Match(paidFilter)
            .Group(new BsonDocument
            {
                { "_id", BsonNull.Value },
                { "totalRevenue", new BsonDocument("$sum", "$totalPrice") }
            })
            .FirstOrDefaultAsync(ct);

        decimal totalRevenue = revenueAggregate != null && revenueAggregate.Contains("totalRevenue")
            ? revenueAggregate["totalRevenue"].ToDecimal()
            : 0;

        var paidOrders = await _orders.CountDocumentsAsync(paidFilter, cancellationToken: ct);
        decimal averageOrderValue = paidOrders > 0 ? totalRevenue / paidOrders : 0;

        var distinctUserIds = await _orders.DistinctAsync<Guid>("UserId", filter, cancellationToken: ct);
        var userList = await distinctUserIds.ToListAsync(ct);
        int newCustomers = userList.Count;

        return new KpiDataDto(
            TotalOrders: totalOrders,
            TotalRevenue: totalRevenue,
            AverageOrderValue: averageOrderValue,
            NewCustomers: newCustomers);
    }
}
