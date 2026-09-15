using nia_api.Enums;

namespace nia_api.Domain.Orders;

public interface IOrderLifecycleService
{
    Task<OrderMutationResult> TransitionStatusAsync(int orderId, EStatus targetStatus, OrderActor actor, CancellationToken ct = default);
    Task<OrderMutationResult> IncreaseStatusAsync(int orderId, OrderActor actor, CancellationToken ct = default);
    Task<OrderMutationResult> DecreaseStatusAsync(int orderId, OrderActor actor, CancellationToken ct = default);
    Task<OrderMutationResult> CancelOrderAsync(int orderId, OrderActor actor, string? rawCancellationToken = null, CancellationToken ct = default);
    Task<OrderMutationResult> MarkPaidAsync(int orderId, OrderActor actor, CancellationToken ct = default);
    Task<OrderMutationResult> UpdateOrderDetailsAsync(
        int orderId,
        string deliveryMethod,
        string? pointId,
        string? pointName,
        string? pointAddress,
        EStatus? targetStatus,
        OrderActor actor,
        CancellationToken ct = default);
}
