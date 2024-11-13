using System.Security.Claims;
using MongoDB.Driver;
using nia_api.Data;
using nia_api.Models;

namespace nia_api.Services;

public class HeaderReaderService
{
    private readonly IMongoCollection<User> _users;

    public HeaderReaderService(NiaDbContext context)
    {
        _users = context.Users;
    }

    public async Task<bool> UserExistsAsync(ClaimsPrincipal user)
    {
        var userId = GetUserIdFromClaims(user);
        if (userId == null)
            return false;

        var dbUser = await _users.Find(u => u.Id == userId).FirstOrDefaultAsync();
        return dbUser != null;
    }
    
    public Task<Guid?> GetUserIdAsync(ClaimsPrincipal user)
    {
        return Task.FromResult(GetUserIdFromClaims(user));
    }
    
    private Guid? GetUserIdFromClaims(ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst("UserId");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return null;
        }
        return userId;
    }
}