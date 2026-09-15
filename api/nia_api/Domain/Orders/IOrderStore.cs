using nia_api.Enums;
using nia_api.Models;

namespace nia_api.Domain.Orders;

public interface IOrderStore
{
    Task<Order?> GetByIdAsync(int orderId, CancellationToken ct = default);
    Task<Order?> GetByCancellationTokenHashAsync(string tokenHash, CancellationToken ct = default);
    Task<Order?> GetByFollowTokenHashAsync(string tokenHash, CancellationToken ct = default);
    Task<List<Order>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<List<Order>> GetAllAsync(CancellationToken ct = default);
    Task<bool> UpdateStatusConditionalAsync(int orderId, EStatus expectedCurrentStatus, EStatus targetStatus, DateTime updatedAt, CancellationToken ct = default);
    Task<bool> UpdateDeliveryDetailsAsync(int orderId, string deliveryMethod, string? pointId, string? pointName, string? pointAddress, DateTime updatedAt, CancellationToken ct = default);
    Task<bool> MarkPaidConditionalAsync(int orderId, EStatus newStatus, DateTime updatedAt, CancellationToken ct = default);
    Task<SalesSummaryDto> GetSalesSummaryAsync(CancellationToken ct = default);
    Task<KpiDataDto> GetKpiDataAsync(CancellationToken ct = default);
}
