namespace Fair.Domain.Trips;

public readonly record struct Money(decimal Amount, string Currency)
{
    public static Money Create(decimal amount, string currency)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency is required.", nameof(currency));

        currency = currency.Trim().ToUpperInvariant();
        if (currency.Length != 3)
            throw new ArgumentException("Currency must be a 3-letter ISO code (e.g. SEK).", nameof(currency));

        return new Money(amount, currency);
    }
}
