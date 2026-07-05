using System.Security.Claims;
using BookStoreApi.Exceptions;

namespace BookStoreApi.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (value is null || !int.TryParse(value, out var id))
            throw new UnauthorizedException("The access token is missing a valid user identifier.");
        return id;
    }

    public static bool IsAdmin(this ClaimsPrincipal principal) => principal.IsInRole("Admin");
}
