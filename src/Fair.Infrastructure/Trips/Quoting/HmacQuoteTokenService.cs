using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Fair.Application.Trips.Quoting;
using Fair.Domain.Trips;
using Microsoft.Extensions.Options;

namespace Fair.Infrastructure.Trips.Quoting;

public sealed class HmacQuoteTokenService : IQuoteTokenService
{
    private readonly QuoteTokenOptions _options;

    public HmacQuoteTokenService(IOptions<QuoteTokenOptions> options)
    {
        _options = options.Value;
    }

    public string CreateToken(TripQuote quote)
    {
        var payload = new QuoteTokenPayload(
            EstimatedDistanceMeters: quote.EstimatedDistanceMeters,
            EstimatedDurationSeconds: quote.EstimatedDurationSeconds,
            Amount: quote.Price.Amount,
            Currency: quote.Price.Currency,
            ExpiresAtUtc: quote.ExpiresAtUtc,
            SurgeMultiplier: quote.SurgeMultiplier
        );

        var json = JsonSerializer.Serialize(payload);
        var bytes = Encoding.UTF8.GetBytes(json);

        var sig = Sign(bytes, _options.Secret);
        return $"{Base64UrlEncode(bytes)}.{Base64UrlEncode(sig)}";
    }

    public bool TryParseToken(string token, out TripQuote quote)
    {
        quote = default!;

        if (!TryValidate(token, out var payload))
            return false;

        var price = new Money(payload.Amount, payload.Currency);

        // Välj gärna Create(...) så vi återanvänder domänens guardrails.
        quote = TripQuote.Create(
            payload.EstimatedDistanceMeters,
            payload.EstimatedDurationSeconds,
            price,
            payload.ExpiresAtUtc,
            payload.SurgeMultiplier
        );

        return true;
    }

    private bool TryValidate(string token, out QuoteTokenPayload payload)
    {
        payload = default!;

        var parts = token.Split('.', 2);
        if (parts.Length != 2) return false;

        var payloadBytes = Base64UrlDecode(parts[0]);
        var sigBytes = Base64UrlDecode(parts[1]);

        var expected = Sign(payloadBytes, _options.Secret);
        if (!CryptographicOperations.FixedTimeEquals(sigBytes, expected))
            return false;

        payload = JsonSerializer.Deserialize<QuoteTokenPayload>(payloadBytes)!;
        return true;
    }

    private sealed record QuoteTokenPayload(
        int EstimatedDistanceMeters,
        int EstimatedDurationSeconds,
        decimal Amount,
        string Currency,
        DateTimeOffset ExpiresAtUtc,
        decimal? SurgeMultiplier
    );

    private static byte[] Sign(byte[] data, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return hmac.ComputeHash(data);
    }

    private static string Base64UrlEncode(byte[] data)
        => Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string s)
    {
        s = s.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }
        return Convert.FromBase64String(s);
    }
}
