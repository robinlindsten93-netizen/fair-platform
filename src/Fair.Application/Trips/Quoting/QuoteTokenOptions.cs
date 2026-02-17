namespace Fair.Application.Trips.Quoting;

public sealed class QuoteTokenOptions
{
    public string Secret { get; init; } = "";
    public int TtlMinutes { get; init; } = 5;
}
