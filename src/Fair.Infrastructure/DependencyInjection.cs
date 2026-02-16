using Fair.Application.Abstractions;
using Fair.Infrastructure.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Fair.Application.Trips;
using Fair.Infrastructure.Trips;
using Fair.Application.Trips.Quoting;
using Fair.Infrastructure.Trips.Quoting;

namespace Fair.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // =========================
        // Auth / JWT
        // =========================
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        // =========================
        // Trips
        // =========================
        services.AddSingleton<ITripRepository, InMemoryTripRepository>();

        // =========================
        // Quoting
        // =========================

        // 🔴 VIKTIG — binder QuoteTokenOptions från appsettings
        services.Configure<QuoteTokenOptions>(
            configuration.GetSection("QuoteToken"));

        services.AddSingleton<ITripQuoteService, TripQuoteService>();
        services.AddSingleton<IQuoteTokenService, HmacQuoteTokenService>();

        // Här fyller vi på senare: db, messaging, logging, etc.
        return services;
    }
}
