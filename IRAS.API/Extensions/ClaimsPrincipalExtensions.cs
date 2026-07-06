// IRAS.API/Extensions/ClaimsPrincipalExtensions.cs
using System.Security.Claims;

namespace IRAS.API.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static int GetUserId(this ClaimsPrincipal user)
        {
            var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(value, out var userId))
                throw new UnauthorizedAccessException("The current user does not have a valid identifier claim.");
            return userId;
        }

        public static string GetRole(this ClaimsPrincipal user)
        {
            return user.FindFirstValue(ClaimTypes.Role)
                ?? throw new UnauthorizedAccessException("The current user does not have a role claim.");
        }
    }
}
