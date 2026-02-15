using Fair.Application.Abstractions;
using Fair.Infrastructure.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fair.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Auth/JWT
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        // Här fyller vi på senare: db, messaging, logging, etc.
        return services;
    }
}
