using nia_api.Domain.Orders;
using nia_api.Enums;
using nia_api.Models;
using Xunit;

namespace nia_api.Tests;

public class OrderLifecyclePolicyTests
{
    private static Order CreateOrder(
        EStatus status = EStatus.PRIJATA,
        string paymentMethod = "Dobierka",
        string paymentStatus = "Pending",
        Guid? userId = null,
        string? rawCancellationToken = null)
    {
        var uid = userId ?? Guid.NewGuid();
        return new Order
        {
            Id = 1001,
            UserId = uid,
            StatusOrder = status,
            PaymentMethod = paymentMethod,
            PaymentStatus = paymentStatus,
            CancellationToken = rawCancellationToken != null ? rawCancellationToken : "token-hash-123",
            CancellationTokenExpiresAt = DateTime.UtcNow.AddDays(1)
        };
    }

    [Fact]
    public void Prijata_To_VoVyrobe_Allowed_For_Dobierka()
    {
        var order = CreateOrder(EStatus.PRIJATA, paymentMethod: "Dobierka", paymentStatus: "Pending");
        var result = OrderLifecyclePolicy.CanTransition(order, EStatus.VO_VYROBE, OrderActor.Staff());

        Assert.True(result.IsAllowed);
    }

    [Fact]
    public void Prijata_To_VoVyrobe_Allowed_When_Paid()
    {
        var order = CreateOrder(EStatus.PRIJATA, paymentMethod: "Stripe", paymentStatus: "Paid");
        var result = OrderLifecyclePolicy.CanTransition(order, EStatus.VO_VYROBE, OrderActor.Staff());

        Assert.True(result.IsAllowed);
    }

    [Fact]
    public void Prijata_To_VoVyrobe_Rejected_When_Unpaid_NonDobierka()
    {
        var order = CreateOrder(EStatus.PRIJATA, paymentMethod: "Stripe", paymentStatus: "Pending");
        var result = OrderLifecyclePolicy.CanTransition(order, EStatus.VO_VYROBE, OrderActor.Staff());

        Assert.False(result.IsAllowed);
        Assert.Equal(PolicyViolation.PaymentRequiredForProduction, result.Violation);
    }

    [Fact]
    public void Prijata_To_VoVyrobe_Rejected_For_Customer()
    {
        var customerId = Guid.NewGuid();
        var order = CreateOrder(EStatus.PRIJATA, paymentMethod: "Dobierka", userId: customerId);
        var result = OrderLifecyclePolicy.CanTransition(order, EStatus.VO_VYROBE, OrderActor.Customer(customerId));

        Assert.False(result.IsAllowed);
        Assert.Equal(PolicyViolation.UnauthorizedActor, result.Violation);
    }

    [Theory]
    [InlineData(EStatus.PRIPRAVENA)]
    [InlineData(EStatus.POSLANA)]
    public void Prijata_SkipStep_Rejected(EStatus targetStatus)
    {
        var order = CreateOrder(EStatus.PRIJATA, paymentMethod: "Dobierka");
        var result = OrderLifecyclePolicy.CanTransition(order, targetStatus, OrderActor.Staff());

        Assert.False(result.IsAllowed);
        Assert.Equal(PolicyViolation.InvalidTransitionSequence, result.Violation);
    }

    [Fact]
    public void VoVyrobe_To_Pripravena_Allowed_For_Staff()
    {
        var order = CreateOrder(EStatus.VO_VYROBE);
        var result = OrderLifecyclePolicy.CanTransition(order, EStatus.PRIPRAVENA, OrderActor.Staff());

        Assert.True(result.IsAllowed);
    }

    [Fact]
    public void VoVyrobe_To_Prijata_Allowed_As_StepBack_For_Staff()
    {
        var order = CreateOrder(EStatus.VO_VYROBE);
        var result = OrderLifecyclePolicy.CanTransition(order, EStatus.PRIJATA, OrderActor.Staff());

        Assert.True(result.IsAllowed);
    }

    [Fact]
    public void Pripravena_To_Poslana_Allowed_For_Staff()
    {
        var order = CreateOrder(EStatus.PRIPRAVENA);
        var result = OrderLifecyclePolicy.CanTransition(order, EStatus.POSLANA, OrderActor.Staff());

        Assert.True(result.IsAllowed);
    }

    [Fact]
    public void Pripravena_To_VoVyrobe_Allowed_As_StepBack_For_Staff()
    {
        var order = CreateOrder(EStatus.PRIPRAVENA);
        var result = OrderLifecyclePolicy.CanTransition(order, EStatus.VO_VYROBE, OrderActor.Staff());

        Assert.True(result.IsAllowed);
    }

    [Fact]
    public void Poslana_To_Pripravena_Allowed_As_StepBack_For_Staff()
    {
        var order = CreateOrder(EStatus.POSLANA);
        var result = OrderLifecyclePolicy.CanTransition(order, EStatus.PRIPRAVENA, OrderActor.Staff());

        Assert.True(result.IsAllowed);
    }

    [Theory]
    [InlineData(EStatus.PRIJATA)]
    [InlineData(EStatus.VO_VYROBE)]
    [InlineData(EStatus.PRIPRAVENA)]
    [InlineData(EStatus.POSLANA)]
    [InlineData(EStatus.ZAPLATENA)]
    public void Terminal_Zrusena_Cannot_Transition_To_Any_Status(EStatus targetStatus)
    {
        var order = CreateOrder(EStatus.ZRUSENA);
        var result = OrderLifecyclePolicy.CanTransition(order, targetStatus, OrderActor.Staff());

        Assert.False(result.IsAllowed);
        Assert.Equal(PolicyViolation.TerminalState, result.Violation);
    }

    [Fact]
    public void Cancel_Prijata_Allowed_For_Customer_Owner()
    {
        var customerId = Guid.NewGuid();
        var order = CreateOrder(EStatus.PRIJATA, userId: customerId);
        var result = OrderLifecyclePolicy.CanCancel(order, OrderActor.Customer(customerId));

        Assert.True(result.IsAllowed);
    }

    [Fact]
    public void Cancel_Prijata_Rejected_For_Different_Customer()
    {
        var customerId = Guid.NewGuid();
        var order = CreateOrder(EStatus.PRIJATA, userId: customerId);
        var result = OrderLifecyclePolicy.CanCancel(order, OrderActor.Customer(Guid.NewGuid()));

        Assert.False(result.IsAllowed);
        Assert.Equal(PolicyViolation.UnauthorizedActor, result.Violation);
    }

    [Fact]
    public void Cancel_Prijata_Allowed_For_Staff()
    {
        var order = CreateOrder(EStatus.PRIJATA);
        var result = OrderLifecyclePolicy.CanCancel(order, OrderActor.Staff());

        Assert.True(result.IsAllowed);
    }

    [Fact]
    public void Cancel_VoVyrobe_Rejected_For_Customer()
    {
        var customerId = Guid.NewGuid();
        var order = CreateOrder(EStatus.VO_VYROBE, userId: customerId);
        var result = OrderLifecyclePolicy.CanCancel(order, OrderActor.Customer(customerId));

        Assert.False(result.IsAllowed);
        Assert.Equal(PolicyViolation.InvalidTransitionSequence, result.Violation);
    }

    [Fact]
    public void Cancel_VoVyrobe_Allowed_For_Staff()
    {
        var order = CreateOrder(EStatus.VO_VYROBE);
        var result = OrderLifecyclePolicy.CanCancel(order, OrderActor.Staff());

        Assert.True(result.IsAllowed);
    }

    [Fact]
    public void Cancel_Already_Cancelled_Rejected()
    {
        var order = CreateOrder(EStatus.ZRUSENA);
        var result = OrderLifecyclePolicy.CanCancel(order, OrderActor.Staff());

        Assert.False(result.IsAllowed);
        Assert.Equal(PolicyViolation.AlreadyCancelled, result.Violation);
    }

    [Fact]
    public void HardDelete_Is_Abolished()
    {
        var order = CreateOrder(EStatus.PRIJATA);
        Assert.False(OrderLifecyclePolicy.CanHardDelete(order));
    }

    [Theory]
    [InlineData(EStatus.PRIJATA, EStatus.VO_VYROBE)]
    [InlineData(EStatus.VO_VYROBE, EStatus.PRIPRAVENA)]
    [InlineData(EStatus.PRIPRAVENA, EStatus.POSLANA)]
    public void GetNextOperationalStatus_Returns_Expected(EStatus current, EStatus expected)
    {
        var order = CreateOrder(current);
        Assert.Equal(expected, OrderLifecyclePolicy.GetNextOperationalStatus(order));
    }

    [Theory]
    [InlineData(EStatus.POSLANA)]
    [InlineData(EStatus.ZRUSENA)]
    public void GetNextOperationalStatus_AtEnd_Returns_Null(EStatus current)
    {
        var order = CreateOrder(current);
        Assert.Null(OrderLifecyclePolicy.GetNextOperationalStatus(order));
    }

    [Theory]
    [InlineData(EStatus.POSLANA, EStatus.PRIPRAVENA)]
    [InlineData(EStatus.PRIPRAVENA, EStatus.VO_VYROBE)]
    [InlineData(EStatus.VO_VYROBE, EStatus.PRIJATA)]
    public void GetPreviousOperationalStatus_Returns_Expected(EStatus current, EStatus expected)
    {
        var order = CreateOrder(current);
        Assert.Equal(expected, OrderLifecyclePolicy.GetPreviousOperationalStatus(order));
    }

    [Theory]
    [InlineData(EStatus.PRIJATA)]
    [InlineData(EStatus.ZRUSENA)]
    public void GetPreviousOperationalStatus_AtStart_Returns_Null(EStatus current)
    {
        var order = CreateOrder(current);
        Assert.Null(OrderLifecyclePolicy.GetPreviousOperationalStatus(order));
    }
}
