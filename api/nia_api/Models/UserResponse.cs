namespace nia_api.Models;

public sealed record UserResponse(
    Guid Id,
    string? Email,
    bool IsEmailConfirmed,
    string? FirstName,
    string? LastName,
    string? Country,
    string? PhoneNumber,
    string? Address,
    string? Zip,
    bool IsAdmin)
{
    public static UserResponse From(User user) => new(
        user.Id, user.Email, user.IsEmailConfirmed, user.FirstName,
        user.LastName, user.Country, user.PhoneNumber, user.Address,
        user.Zip, user.IsAdmin);
}
