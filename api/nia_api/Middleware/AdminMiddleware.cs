using nia_api.Services;

namespace nia_api.Middleware;

public class AdminMiddleware
{
    private readonly RequestDelegate _next;

    public AdminMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/admin"))
        {
            var isAdmin = context.User?.Claims.FirstOrDefault(c => c.Type == "admin")?.Value;

            if (string.IsNullOrEmpty(isAdmin) || isAdmin.ToLower() != "true")
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "Access denied. Admins only." });
                return;
            }
        }
        
        await _next(context);
    }
}