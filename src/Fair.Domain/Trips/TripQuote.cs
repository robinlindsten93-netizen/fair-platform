namespace Fair.Domain.Trips;

public sealed record TripQuote(
    int EstimatedDistanceMeters,
    int EstimatedDurationSeconds,
    Money Price,
    DateTimeOffset ExpiresAtUtc,
    decimal? SurgeMultiplier = null)
{
    public bool IsExpired(DateTimeOffset nowUtc) => nowUtc >= ExpiresAtUtc;

    public static TripQuote Create(
        int estimatedDistanceMeters,
        int estimatedDurationSeconds,
        Money price,
        DateTimeOffset expiresAtUtc,
        decimal? surgeMultiplier = null)
    {
        if (estimatedDistanceMeters <= 0)
            throw new ArgumentOutOfRangeException(nameof(estimatedDistanceMeters));

        if (estimatedDurationSeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(estimatedDurationSeconds));

        if (expiresAtUtc <= DateTimeOffset.UtcNow)
            throw new ArgumentOutOfRangeException(nameof(expiresAtUtc), "Quote must expire in the future.");

        if (surgeMultiplier is not null && surgeMultiplier <= 0)
            throw new ArgumentOutOfRangeException(nameof(surgeMultiplier));

        return new TripQuote(
            estimatedDistanceMeters,
            estimatedDurationSeconds,
            price,
            expiresAtUtc,
            surgeMultiplier);
    }
}
