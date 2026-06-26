using Microsoft.Extensions.DependencyInjection;

namespace HireIQ.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Application-layer service registrations (use cases, validators, etc.)
        // Concrete service implementations live in Infrastructure or here once split out.
        return services;
    }
}
