namespace nia_api.Domain.Orders;

public sealed record SalesSummaryDto(
    long SoldOrders,
    long PendingOrders,
    long MakingOrders,
    long ReadyOrders,
    long SendOrders,
    long CancelOrders);

public sealed record KpiDataDto(
    long TotalOrders,
    decimal TotalRevenue,
    decimal AverageOrderValue,
    int NewCustomers);
