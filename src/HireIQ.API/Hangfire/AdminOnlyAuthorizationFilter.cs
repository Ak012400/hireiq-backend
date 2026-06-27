using Hangfire.Dashboard;

namespace HireIQ.API.Hangfire;

/// <summary>
/// Hangfire dashboard is publicly mounted at /hangfire — lock it to authenticated admin users only.
/// </summary>
public sealed class AdminOnlyAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        if (!httpContext.User.Identity?.IsAuthenticated ?? true) return false;
        // Adjust role name to match your seeded admin role.
        return httpContext.User.IsInRole("admin") || httpContext.User.IsInRole("hirer");
    }
}
