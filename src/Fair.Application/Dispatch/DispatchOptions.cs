namespace Fair.Application.Dispatch;

public sealed class DispatchOptions
{
    // Totalt max offers per trip (summa över alla waves)
    public int MaxOffersPerTrip { get; init; } = 5;

    public int MaxSearchRadiusMeters { get; init; } = 6000; // 6 km
    public int LocationMaxAgeSeconds { get; init; } = 45;   // “färsk position”
    public int OfferTtlSeconds { get; init; } = 20;

    // =========================
    // WAVES (NEW)
    // =========================
    // Hur många offers i första vågen
    public int Wave1Count { get; init; } = 2;

    // Hur många offers per extra våg
    public int WaveNCount { get; init; } = 2;

    // Delay mellan vågor (sekunder)
    public int WaveDelaySeconds { get; init; } = 3;
}