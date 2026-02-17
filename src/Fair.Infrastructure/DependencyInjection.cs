using Fair.Application.Abstractions;
using Fair.Application.Auth;
using Fair.Application.Trips;
using Fair.Application.Trips.Quoting;
using Fair.Infrastructure.Auth;
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
        // AuthZ / Roles (för /me + senare RBAC)
        // =========================
        services.AddSingleton<InMemoryRoleAssignmentRepository>();

        services.AddSingleton<IRoleAssignmentRepository>(sp => sp.GetRequiredService<InMemoryRoleAssignmentRepository>());
        services.AddSingleton<IRoleAssignmentWriter>(sp => sp.GetRequiredService<InMemoryRoleAssignmentRepository>());


        // =========================
        // Trips
        // =========================
        services.AddSingleton<ITripRepository, InMemoryTripRepository>();

        // =========================
        // Quoting
        // =========================

        // Binder QuoteTokenOptions från appsettings
        services.Configure<QuoteTokenOptions>(config.GetSection("QuoteToken"));

        services.AddSingleton<ITripQuoteService, TripQuoteService>();
        services.AddSingleton<IQuoteTokenService, HmacQuoteTokenService>();

        // Här fyller vi på senare: db, messaging, logging, etc.
        return services;
    }
}

