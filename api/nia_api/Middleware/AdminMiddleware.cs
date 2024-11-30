using nia_api.Services;

namespace nia_api.Middleware;

public class RoleMiddleware
{
    private readonly RequestDelegate _next;

    private RoleMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/admin"))
        {
            var userRole = context.User?.Claims.FirstOrDefault(c => c.Type == "role")?.Value;

            if (string.IsNullOrEmpty(userRole) || userRole != "admin")
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "Access denied. Admins only." });
                return;
            }
        }
        
        await _next(context);
    }
}