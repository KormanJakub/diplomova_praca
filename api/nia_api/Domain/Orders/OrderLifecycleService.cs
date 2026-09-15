using nia_api.Enums;
using nia_api.Models;
using nia_api.Security;
using nia_api.Services;

namespace nia_api.Domain.Orders;

public class OrderLifecycleService : IOrderLifecycleService
{
    private readonly IOrderStore _orderStore;
    private readonly OrderService _orderService;

    public OrderLifecycleService(IOrderStore orderStore, OrderService orderService)
    {
        _orderStore = orderStore;
        _orderService = orderService;
    }

    public async Task<OrderMutationResult> TransitionStatusAsync(
        int orderId,
        EStatus targetStatus,
        OrderActor actor,
        CancellationToken ct = default)
    {
        var order = await _orderStore.GetByIdAsync(orderId, ct);
        if (order == null)
            return OrderMutationResult.NotFound();

        var evaluation = OrderLifecyclePolicy.CanTransition(order, targetStatus, actor);
        if (!evaluation.IsAllowed)
        {
            return evaluation.Violation switch
            {
                PolicyViolation.UnauthorizedActor => OrderMutationResult.Forbidden(evaluation.Reason ?? "Prístup odmietnutý."),
                PolicyViolation.TerminalState or PolicyViolation.AlreadyCancelled => OrderMutationResult.Invalid(evaluation.Reason ?? "Objednávka je zrušená."),
                PolicyViolation.PaymentRequiredForProduction => OrderMutationResult.Invalid(evaluation.Reason ?? "Platba je povinná."),
                _ => OrderMutationResult.Invalid(evaluation.Reason ?? "Neplatný prechod stavu.")
            };
        }

        if (targetStatus == EStatus.ZRUSENA)
        {
            return await CancelOrderAsync(orderId, actor, ct: ct);
        }

        var success = await _orderStore.UpdateStatusConditionalAsync(
            orderId,
            order.StatusOrder,
            targetStatus,
            DateTime.UtcNow,
            ct);

        if (!success)
        {
            return OrderMutationResult.Conflict("Stav objednávky bol medzitým zmenený inou požiadavkou.");
        }

        var updatedOrder = await _orderStore.GetByIdAsync(orderId, ct);
        return OrderMutationResult.Ok(updatedOrder!, $"Stav objednávky bol zmenený na {targetStatus}.");
    }

    public async Task<OrderMutationResult> IncreaseStatusAsync(
        int orderId,
        OrderActor actor,
        CancellationToken ct = default)
    {
        var order = await _orderStore.GetByIdAsync(orderId, ct);
        if (order == null)
            return OrderMutationResult.NotFound();

        var nextStatus = OrderLifecyclePolicy.GetNextOperationalStatus(order);
        if (nextStatus == null)
        {
            return OrderMutationResult.Invalid("Objednávka už dosiahla konečný stav alebo je zrušená.");
        }

        var transitionResult = await TransitionStatusAsync(orderId, nextStatus.Value, actor, ct);
        if (transitionResult.IsSuccess)
        {
            return OrderMutationResult.Ok(transitionResult.Order!, "Order status increased!");
        }

        return transitionResult;
    }

    public async Task<OrderMutationResult> DecreaseStatusAsync(
        int orderId,
        OrderActor actor,
        CancellationToken ct = default)
    {
        var order = await _orderStore.GetByIdAsync(orderId, ct);
        if (order == null)
            return OrderMutationResult.NotFound();

        var prevStatus = OrderLifecyclePolicy.GetPreviousOperationalStatus(order);
        if (prevStatus == null)
        {
            return OrderMutationResult.Invalid("Objednávka nemôže byť vrátená do predchádzajúceho stavu.");
        }

        var transitionResult = await TransitionStatusAsync(orderId, prevStatus.Value, actor, ct);
        if (transitionResult.IsSuccess)
        {
            return OrderMutationResult.Ok(transitionResult.Order!, "Order status decreased!");
        }

        return transitionResult;
    }

    public async Task<OrderMutationResult> CancelOrderAsync(
        int orderId,
        OrderActor actor,
        string? rawCancellationToken = null,
        CancellationToken ct = default)
    {
        if (actor.IsGuest)
        {
            if (string.IsNullOrWhiteSpace(rawCancellationToken))
                return OrderMutationResult.Invalid("Cancellation token is required!");

            var tokenHash = CapabilityToken.Hash(rawCancellationToken);
            var order = await _orderStore.GetByCancellationTokenHashAsync(tokenHash, ct);
            if (order == null || (order.CancellationTokenExpiresAt.HasValue && order.CancellationTokenExpiresAt.Value <= DateTime.UtcNow))
                return OrderMutationResult.NotFound("Invalid or expired token!");

            if (orderId > 0 && order.Id != orderId)
                return OrderMutationResult.NotFound("Invalid or expired token!");

            var eval = OrderLifecyclePolicy.CanCancel(order, actor);
            if (!eval.IsAllowed)
                return OrderMutationResult.NotFound("Invalid or expired token!");

            var cancelled = await _orderService.CancelByTokenAsync(rawCancellationToken);
            if (!cancelled)
                return OrderMutationResult.NotFound("Invalid or expired token!");

            var updatedGuestOrder = await _orderStore.GetByIdAsync(order.Id, ct);
            return OrderMutationResult.Ok(updatedGuestOrder ?? order, "Order canceled successfully!");
        }

        if (actor.IsCustomer)
        {
            if (!actor.UserId.HasValue)
                return OrderMutationResult.Forbidden();

            var order = await _orderStore.GetByIdAsync(orderId, ct);
            if (order == null || order.UserId != actor.UserId.Value)
                return OrderMutationResult.NotFound();

            var eval = OrderLifecyclePolicy.CanCancel(order, actor);
            if (!eval.IsAllowed)
                return OrderMutationResult.Invalid(eval.Reason ?? "Objednávku nie je možné zrušiť.");

            var cancelled = await _orderService.CancelByUserAsync(orderId, actor.UserId.Value);
            if (!cancelled)
                return OrderMutationResult.NotFound();

            var updatedCustomerOrder = await _orderStore.GetByIdAsync(orderId, ct);
            return OrderMutationResult.Ok(updatedCustomerOrder ?? order, "Objednávka bola úspešne zrušená.");
        }

        if (actor.IsStaff)
        {
            var order = await _orderStore.GetByIdAsync(orderId, ct);
            if (order == null)
                return OrderMutationResult.NotFound();

            var eval = OrderLifecyclePolicy.CanCancel(order, actor);
            if (!eval.IsAllowed)
                return OrderMutationResult.Invalid(eval.Reason ?? "Objednávku nie je možné zrušiť.");

            var cancelled = await _orderService.CancelByAdminAsync(orderId);
            if (!cancelled)
                return OrderMutationResult.NotFound("Order not found or already cancelled!");

            var updatedAdminOrder = await _orderStore.GetByIdAsync(orderId, ct);
            return OrderMutationResult.Ok(updatedAdminOrder ?? order, "Order cancelled!");
        }

        return OrderMutationResult.Forbidden();
    }

    public async Task<OrderMutationResult> MarkPaidAsync(
        int orderId,
        OrderActor actor,
        CancellationToken ct = default)
    {
        if (!actor.IsStaff && !actor.IsSystem)
            return OrderMutationResult.Forbidden();

        var order = await _orderStore.GetByIdAsync(orderId, ct);
        if (order == null)
            return OrderMutationResult.NotFound();

        if (order.StatusOrder == EStatus.ZRUSENA)
            return OrderMutationResult.Invalid("Zrušenú objednávku nie je možné označiť ako zaplatenú.");

        if (string.Equals(order.PaymentMethod, "Stripe", StringComparison.OrdinalIgnoreCase) && !actor.IsSystem)
            return OrderMutationResult.Invalid("Stripe platbu môže potvrdiť iba podpísaný webhook.");

        var newStatus = order.StatusOrder == EStatus.PRIJATA ? EStatus.ZAPLATENA : order.StatusOrder;
        var success = await _orderStore.MarkPaidConditionalAsync(orderId, newStatus, DateTime.UtcNow, ct);
        if (!success)
            return OrderMutationResult.Conflict("Objednávku sa nepodarilo označiť ako zaplatenú.");

        var updatedOrder = await _orderStore.GetByIdAsync(orderId, ct);
        return OrderMutationResult.Ok(updatedOrder ?? order, "Platba objednávky bola úspešne potvrdená.");
    }

    public async Task<OrderMutationResult> UpdateOrderDetailsAsync(
        int orderId,
        string deliveryMethod,
        string? pointId,
        string? pointName,
        string? pointAddress,
        EStatus? targetStatus,
        OrderActor actor,
        CancellationToken ct = default)
    {
        if (!actor.IsStaff)
            return OrderMutationResult.Forbidden();

        var order = await _orderStore.GetByIdAsync(orderId, ct);
        if (order == null)
            return OrderMutationResult.NotFound();

        await _orderStore.UpdateDeliveryDetailsAsync(
            orderId,
            deliveryMethod,
            pointId,
            pointName,
            pointAddress,
            DateTime.UtcNow,
            ct);

        if (targetStatus.HasValue && targetStatus.Value != order.StatusOrder)
        {
            var transitionResult = await TransitionStatusAsync(orderId, targetStatus.Value, actor, ct);
            if (!transitionResult.IsSuccess)
                return transitionResult;
        }

        var updatedOrder = await _orderStore.GetByIdAsync(orderId, ct);
        return OrderMutationResult.Ok(updatedOrder ?? order, "Order updated!");
    }
}
