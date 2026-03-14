using System.Security.Claims;

namespace EMenu.Web.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static string GetAuditUserId(this ClaimsPrincipal? user)
        {
            return user?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Anonymous";
        }

        public static string GetAuditUserName(this ClaimsPrincipal? user)
        {
            return user?.Identity?.Name ?? "Anonymous";
        }

        public static string GetAuditRoles(this ClaimsPrincipal? user)
        {
            var roles = user?
                .FindAll(ClaimTypes.Role)
                .Select(x => x.Value)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToArray();

            return roles == null || roles.Length == 0
                ? "Anonymous"
                : string.Join(",", roles);
        }
    }
}
