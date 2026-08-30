using System.Security.Claims;
using TaskManagementApi.Interfaces;

namespace TaskManagementApi.Services;

public class CurrentUser : ICurrentUser
{
    private readonly int? _userId;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        var value = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(value, out var id))
        {
            _userId = id;
        }
    }

    public int GetRequiredUserId()
    {
        return _userId
            ?? throw new InvalidOperationException("Authenticated user is required.");
    }
}
