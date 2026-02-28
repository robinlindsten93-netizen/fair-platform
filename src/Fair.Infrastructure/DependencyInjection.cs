using Fair.Application.Abstractions;
using Fair.Application.Auth;
using Fair.Application.Dispatch;
using Fair.Application.Drivers;
using Fair.Application.Trips;
using Fair.Application.Trips.Quoting;
using Fair.Infrastructure.Auth;
using Fair.Infrastructure.Dispatch;
using Fair.Infrastructure.Drivers;
using Fair.Infrastructure.Trips;
using Fair.Infrastructure.Trips.Quoting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Fair.Application.Trips.Guards;
using Fair.Application.Trips.Queries;
using Fair.Infrastructure.Trips.Queries;

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
        // AuthZ / Roles (SINGLETON SHARED)
        // =========================
        services.AddSingleton<InMemoryRoleAssignmentRepository>();

        services.AddSingleton<IRoleAssignmentRepository>(sp =>
            sp.GetRequiredService<InMemoryRoleAssignmentRepository>());

        services.AddSingleton<IRoleAssignmentWriter>(sp =>
            sp.GetRequiredService<InMemoryRoleAssignmentRepository>());

        // =========================
        // Drivers (CRITICAL SINGLETON SHARING)
        // =========================
        services.AddSingleton<InMemoryDriverProfileRepository>();

        services.AddSingleton<IDriverProfileRepository>(sp =>
            sp.GetRequiredService<InMemoryDriverProfileRepository>());

        services.AddSingleton<IDriverAvailabilityQuery>(sp =>
            sp.GetRequiredService<InMemoryDriverProfileRepository>());

        // =========================
        // Dispatch (offers + assignments)
        // =========================
        services.Configure<DispatchOptions>(config.GetSection("Dispatch"));
        services.AddHostedService<DispatchOfferExpiryService>();

        services.AddSingleton<IDispatchOfferRepository, InMemoryDispatchOfferRepository>();
        services.AddSingleton<IDriverAssignmentRepository, InMemoryDriverAssignmentRepository>();

        // Dispatch use cases
        services.AddScoped<CreateDispatchOffers>();
        services.AddScoped<GetMyOffers>();
        services.AddScoped<AcceptDispatchOffer>();

        // =========================
        // Trips
        // =========================

        // Trips (write)
        services.AddSingleton<ITripRepository, InMemoryTripRepository>();

        // Trips (read)
        services.AddSingleton<ITripReadRepository, InMemoryTripReadRepository>();

        // Guards (Application)
        services.AddScoped<ActiveTripGuard>();

        // =========================
        // Quoting
        // =========================
        services.Configure<QuoteTokenOptions>(config.GetSection("QuoteToken"));
        services.AddSingleton<ITripQuoteService, TripQuoteService>();
        services.AddSingleton<IQuoteTokenService, HmacQuoteTokenService>();

        return services;
    }
}