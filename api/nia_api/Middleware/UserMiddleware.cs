using nia_api.Services;

namespace nia_api.Middleware;

public class UserMiddleware
{
    private readonly RequestDelegate _next;
    private readonly HeaderReaderService _headerReader;
    
    public UserMiddleware(RequestDelegate next, HeaderReaderService headerReader)
    {
        _next = next;
        _headerReader = headerReader;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/user"))
        {
            var user = context.User;

            var userExists = await _headerReader.UserExistsAsync(user);
            
            if (!userExists)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "User does not exist." });
                return;
            }
        }
        
        await _next(context);
    }
}