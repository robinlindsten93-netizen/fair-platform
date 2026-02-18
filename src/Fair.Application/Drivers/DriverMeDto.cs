namespace Fair.Application.Drivers;

public sealed record DriverMeDto(
    string UserId,
    bool IsOnline,
    DateTimeOffset LastSeenUtc
);
