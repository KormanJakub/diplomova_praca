using nia_api.Enums;
using nia_api.Models;

namespace nia_api.Domain.Orders;

public enum PolicyViolation
{
    None,
    TerminalState,
    SameState,
    PaymentRequiredForProduction,
    UnauthorizedActor,
    InvalidTransitionSequence,
    OrderNotFound,
    AlreadyCancelled
}

public sealed record PolicyEvaluation(bool IsAllowed, PolicyViolation Violation = PolicyViolation.None, string? Reason = null)
{
    public static PolicyEvaluation Allowed() => new(true);
    public static PolicyEvaluation Denied(PolicyViolation violation, string reason) => new(false, violation, reason);
}

public static class OrderLifecyclePolicy
{
    public static PolicyEvaluation CanTransition(Order order, EStatus targetStatus, OrderActor actor)
    {
        if (order == null)
            return PolicyEvaluation.Denied(PolicyViolation.OrderNotFound, "Order does not exist.");

        var currentStatus = order.StatusOrder;

        if (currentStatus == targetStatus)
            return PolicyEvaluation.Denied(PolicyViolation.SameState, "Order is already in the requested status.");

        // Rule 1: Terminal state - once cancelled, no mutation is permitted
        if (currentStatus == EStatus.ZRUSENA)
            return PolicyEvaluation.Denied(PolicyViolation.TerminalState, "Zrušenú objednávku nie je možné meniť.");

        // Rule 2: Cancellation transition (to ZRUSENA)
        if (targetStatus == EStatus.ZRUSENA)
        {
            return CanCancel(order, actor);
        }

        // Rule 3: Transitions to other operational states require Staff or System privileges
        if (!actor.IsStaff && actor.Type != OrderActorType.SystemCallback)
        {
            return PolicyEvaluation.Denied(PolicyViolation.UnauthorizedActor, "Iba personál môže meniť prevádzkový stav objednávky.");
        }

        // Rule 4: Payment verification before entering production (VO_VYROBE)
        if (targetStatus == EStatus.VO_VYROBE)
        {
            var isPaid = string.Equals(order.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase);
            var isCashOnDelivery = string.Equals(order.PaymentMethod, "Dobierka", StringComparison.OrdinalIgnoreCase);

            if (!isPaid && !isCashOnDelivery)
            {
                return PolicyEvaluation.Denied(
                    PolicyViolation.PaymentRequiredForProduction,
                    "Unpaid online or bank-transfer order cannot enter production.");
            }
        }

        // Rule 5: Allowed forward and operational transition matrix
        var isPermittedSequence = (currentStatus, targetStatus) switch
        {
            // PRIJATA -> ZAPLATENA (Paid manually or by webhook)
            (EStatus.PRIJATA, EStatus.ZAPLATENA) => true,

            // PRIJATA -> VO_VYROBE (Allowed if Dobierka or already paid, checked above)
            (EStatus.PRIJATA, EStatus.VO_VYROBE) => true,

            // ZAPLATENA -> VO_VYROBE (Production started)
            (EStatus.ZAPLATENA, EStatus.VO_VYROBE) => true,

            // VO_VYROBE -> PRIPRAVENA (Production finished, packed)
            (EStatus.VO_VYROBE, EStatus.PRIPRAVENA) => true,

            // PRIPRAVENA -> POSLANA (Dispatched/shipped)
            (EStatus.PRIPRAVENA, EStatus.POSLANA) => true,

            // Operational backwards corrections by staff
            (EStatus.POSLANA, EStatus.PRIPRAVENA) => actor.IsStaff,
            (EStatus.PRIPRAVENA, EStatus.VO_VYROBE) => actor.IsStaff,
            (EStatus.VO_VYROBE, EStatus.ZAPLATENA) => actor.IsStaff && string.Equals(order.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase),
            (EStatus.VO_VYROBE, EStatus.PRIJATA) => actor.IsStaff,

            // Claims from completed order
            (EStatus.POSLANA, EStatus.REKLAMACIA) => true,

            _ => false
        };

        if (!isPermittedSequence)
        {
            return PolicyEvaluation.Denied(
                PolicyViolation.InvalidTransitionSequence,
                $"Prechod zo stavu {currentStatus} do stavu {targetStatus} nie je povolený.");
        }

        return PolicyEvaluation.Allowed();
    }

    public static PolicyEvaluation CanCancel(Order order, OrderActor actor)
    {
        if (order == null)
            return PolicyEvaluation.Denied(PolicyViolation.OrderNotFound, "Order does not exist.");

        if (order.StatusOrder == EStatus.ZRUSENA)
            return PolicyEvaluation.Denied(PolicyViolation.AlreadyCancelled, "Order is already cancelled.");

        if (actor.IsStaff)
        {
            // Staff can cancel orders in PRIJATA, ZAPLATENA, or VO_VYROBE
            if (order.StatusOrder is EStatus.PRIJATA or EStatus.ZAPLATENA or EStatus.VO_VYROBE)
                return PolicyEvaluation.Allowed();

            return PolicyEvaluation.Denied(
                PolicyViolation.InvalidTransitionSequence,
                "Odoslanú alebo vybavenú objednávku nie je možné zrušiť; použite reklamáciu.");
        }

        if (actor.IsCustomer)
        {
            // Customer can only cancel their own order in PRIJATA status
            if (order.UserId != actor.UserId)
                return PolicyEvaluation.Denied(PolicyViolation.UnauthorizedActor, "Objednávka nepatrí tomuto používateľovi.");

            if (order.StatusOrder != EStatus.PRIJATA)
                return PolicyEvaluation.Denied(PolicyViolation.InvalidTransitionSequence, "Objednávku vo výrobe alebo odoslanú už nie je možné zrušiť zákazníkom.");

            return PolicyEvaluation.Allowed();
        }

        if (actor.IsGuest)
        {
            // Guest can cancel via capability token only if in PRIJATA status
            if (order.StatusOrder != EStatus.PRIJATA)
                return PolicyEvaluation.Denied(PolicyViolation.InvalidTransitionSequence, "Objednávku nie je možné zrušiť v aktuálnom stave.");

            return PolicyEvaluation.Allowed();
        }

        return PolicyEvaluation.Denied(PolicyViolation.UnauthorizedActor, "Neautorizovaný pokus o zrušenie.");
    }

    public static EStatus? GetNextOperationalStatus(Order order)
    {
        if (order == null || order.StatusOrder == EStatus.ZRUSENA)
            return null;

        return order.StatusOrder switch
        {
            EStatus.PRIJATA => string.Equals(order.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(order.PaymentMethod, "Dobierka", StringComparison.OrdinalIgnoreCase)
                ? EStatus.VO_VYROBE
                : EStatus.ZAPLATENA,
            EStatus.ZAPLATENA => EStatus.VO_VYROBE,
            EStatus.VO_VYROBE => EStatus.PRIPRAVENA,
            EStatus.PRIPRAVENA => EStatus.POSLANA,
            _ => null
        };
    }

    public static EStatus? GetPreviousOperationalStatus(Order order)
    {
        if (order == null || order.StatusOrder == EStatus.ZRUSENA)
            return null;

        return order.StatusOrder switch
        {
            EStatus.POSLANA => EStatus.PRIPRAVENA,
            EStatus.PRIPRAVENA => EStatus.VO_VYROBE,
            EStatus.VO_VYROBE => string.Equals(order.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase)
                ? EStatus.ZAPLATENA
                : EStatus.PRIJATA,
            _ => null
        };
    }

    public static bool CanHardDelete(Order? order) => false;
}
