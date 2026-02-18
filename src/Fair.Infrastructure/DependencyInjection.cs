using Fair.Application.Abstractions;
using Fair.Application.Auth;
using Fair.Application.Drivers;
using Fair.Application.Trips;
using Fair.Application.Trips.Quoting;
using Fair.Infrastructure.Auth;
using Fair.Infrastructure.Drivers;
using Fair.Infrastructure.Trips;
using Fair.Infrastructure.Trips.Quoting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fair.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // =========================
        // Auth / JWT
        // =========================
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        // =========================
        // AuthZ / Roles (för /me + policies)
        // =========================
        services.AddSingleton<InMemoryRoleAssignmentRepository>();
        services.AddSingleton<IRoleAssignmentRepository>(sp => sp.GetRequiredService<InMemoryRoleAssignmentRepository>());
        services.AddSingleton<IRoleAssignmentWriter>(sp => sp.GetRequiredService<InMemoryRoleAssignmentRepository>());

        // =========================
        // Drivers (availability foundation)
        // =========================
        services.AddSingleton<IDriverProfileRepository, InMemoryDriverProfileRepository>();

        // =========================
        // Trips
        // =========================
        services.AddSingleton<ITripRepository, InMemoryTripRepository>();

        // =========================
        // Quoting
        // =========================
        services.Configure<QuoteTokenOptions>(config.GetSection("QuoteToken"));
        services.AddSingleton<ITripQuoteService, TripQuoteService>();
        services.AddSingleton<IQuoteTokenService, HmacQuoteTokenService>();

        return services;
    }
}

