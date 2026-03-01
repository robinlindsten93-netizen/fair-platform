using Fair.Application.Abstractions;
using Fair.Application.Auth;
using Fair.Application.Dispatch;
using Fair.Application.Drivers;
using Fair.Application.Trips;
using Fair.Application.Trips.Guards;
using Fair.Application.Trips.Queries;
using Fair.Application.Trips.Quoting;

using Fair.Infrastructure.Auth;
using Fair.Infrastructure.Dispatch;
using Fair.Infrastructure.Drivers;
using Fair.Infrastructure.Trips;
using Fair.Infrastructure.Trips.Queries;
using Fair.Infrastructure.Trips.Quoting;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using AppDispatchOptions = Fair.Application.Dispatch.DispatchOptions;

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
        // Driver locations (DISPATCH) — SINGLETON SHARED
        // =========================
        services.AddSingleton<InMemoryDriverLocationRepository>();

        services.AddSingleton<IDriverLocationQuery>(sp =>
            sp.GetRequiredService<InMemoryDriverLocationRepository>());

        services.AddSingleton<IDriverLocationWriter>(sp =>
            sp.GetRequiredService<InMemoryDriverLocationRepository>());

        // =========================
        // Trips
        // =========================
        services.AddSingleton<ITripRepository, InMemoryTripRepository>();
        services.AddSingleton<ITripReadRepository, InMemoryTripReadRepository>();

        // Guards (Application)
        services.AddScoped<ActiveTripGuard>();

        // =========================
        // Dispatch options (Application) — SINGLETON INSTANCE
        // =========================
        var dispatchOpt = new AppDispatchOptions();
        config.GetSection("Dispatch").Bind(dispatchOpt);
        services.AddSingleton(dispatchOpt); // register as AppDispatchOptions

        // =========================
        // Dispatch storage
        // =========================
        services.AddSingleton<IDispatchOfferRepository, InMemoryDispatchOfferRepository>();
        services.AddSingleton<IDriverAssignmentRepository, InMemoryDriverAssignmentRepository>();

        // Wave queue + worker
        services.AddSingleton<DispatchWaveQueue>();
        services.AddHostedService<DispatchWaveService>();

        // =========================
        // Dispatch use cases
        // =========================
        services.AddScoped<CreateDispatchOffers>(sp =>
        {
            var queue = sp.GetRequiredService<DispatchWaveQueue>();

            return new CreateDispatchOffers(
                trips: sp.GetRequiredService<ITripRepository>(),
                offers: sp.GetRequiredService<IDispatchOfferRepository>(),
                assignments: sp.GetRequiredService<IDriverAssignmentRepository>(),
                availability: sp.GetRequiredService<IDriverAvailabilityQuery>(),
                locations: sp.GetRequiredService<IDriverLocationQuery>(),
                opt: sp.GetRequiredService<AppDispatchOptions>(),
                scheduleNextWave: (tripId, version, nextAt) => queue.Schedule(tripId, version, nextAt)
            );
        });

        services.AddScoped<GetMyOffers>();
        services.AddScoped<AcceptDispatchOffer>();

        // =========================
        // Quoting
        // =========================
        services.Configure<QuoteTokenOptions>(config.GetSection("QuoteToken"));
        services.AddSingleton<ITripQuoteService, TripQuoteService>();
        services.AddSingleton<IQuoteTokenService, HmacQuoteTokenService>();

        return services;
    }
}