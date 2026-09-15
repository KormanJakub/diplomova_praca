using nia_api.Models;

namespace nia_api.Domain.Orders;

public enum OrderMutationStatus
{
    Success,
    NotFound,
    Forbidden,
    Conflict,
    InvalidData
}

public sealed record OrderMutationResult(
    OrderMutationStatus Status,
    string Message,
    Order? Order = null)
{
    public bool IsSuccess => Status == OrderMutationStatus.Success;

    public static OrderMutationResult Ok(Order order, string message = "Úspešne vykonané.") =>
        new(OrderMutationStatus.Success, message, order);

    public static OrderMutationResult NotFound(string message = "Order not found!") =>
        new(OrderMutationStatus.NotFound, message);

    public static OrderMutationResult Forbidden(string message = "Prístup odmietnutý.") =>
        new(OrderMutationStatus.Forbidden, message);

    public static OrderMutationResult Conflict(string message) =>
        new(OrderMutationStatus.Conflict, message);

    public static OrderMutationResult Invalid(string message) =>
        new(OrderMutationStatus.InvalidData, message);
}
