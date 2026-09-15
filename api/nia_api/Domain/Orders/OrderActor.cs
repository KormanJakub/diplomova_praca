namespace nia_api.Domain.Orders;

public enum OrderActorType
{
    Customer,
    Guest,
    Staff,
    SystemCallback
}

public sealed record OrderActor(OrderActorType Type, Guid? UserId = null, string? Token = null, string? StaffRole = null)
{
    public static OrderActor Customer(Guid userId) => new(OrderActorType.Customer, UserId: userId);
    public static OrderActor Guest(string token) => new(OrderActorType.Guest, Token: token);
    public static OrderActor Staff(Guid? userId = null, string role = "admin") => new(OrderActorType.Staff, UserId: userId, StaffRole: role);
    public static OrderActor System(string provider = "system") => new(OrderActorType.SystemCallback, Token: provider);

    public bool IsStaff => Type == OrderActorType.Staff;
    public bool IsCustomer => Type == OrderActorType.Customer;
    public bool IsGuest => Type == OrderActorType.Guest;
    public bool IsSystem => Type == OrderActorType.SystemCallback;
}
