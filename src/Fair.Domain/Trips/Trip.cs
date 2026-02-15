namespace Fair.Domain.Trips;

public sealed class Trip
{
    public Guid Id { get; }
    public Guid RiderId { get; }
    public TransportMode Mode { get; }
    public TripStatus Status { get; private set; }

    public Location Pickup { get; private set; }
    public Location Dropoff { get; private set; }

    public TripQuote? Quote { get; private set; }

    public Guid? DriverId { get; private set; }
    public string? VehicleId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private Trip(Guid id, Guid riderId, Location pickup, Location dropoff, TransportMode mode, DateTimeOffset nowUtc)
    {
        Id = id;
        RiderId = riderId;
        Pickup = pickup;
        Dropoff = dropoff;
        Mode = mode;

        Status = TripStatus.Draft;
        CreatedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
    }

    public static Trip CreateDraft(Guid riderId, Location pickup, Location dropoff, TransportMode mode, DateTimeOffset? nowUtc = null)
    {
        if (riderId == Guid.Empty) throw new ArgumentException("RiderId is required.", nameof(riderId));

        var now = nowUtc ?? DateTimeOffset.UtcNow;
        return new Trip(Guid.NewGuid(), riderId, pickup, dropoff, mode, now);
    }

    public void ApplyQuote(TripQuote quote, DateTimeOffset? nowUtc = null)
    {
        EnsureNotFinal();

        if (Status is not (TripStatus.Draft or TripStatus.Quoted))
            throw new InvalidOperationException($"Cannot apply quote when status is {Status}.");

        Quote = quote;
        Status = TripStatus.Quoted;
        Touch(nowUtc);
    }

    public void Request(DateTimeOffset? nowUtc = null)
    {
        EnsureNotFinal();

        if (Status != TripStatus.Quoted)
            throw new InvalidOperationException("Trip must be Quoted before it can be Requested.");

        var now = nowUtc ?? DateTimeOffset.UtcNow;

        if (Quote is null)
            throw new InvalidOperationException("Missing quote.");

        if (Quote.IsExpired(now))
        {
            Status = TripStatus.Expired;
            Touch(now);
            throw new InvalidOperationException("Quote is expired.");
        }

        Status = TripStatus.Requested;
        Touch(now);
    }

    public void Accept(Guid driverId, string vehicleId, DateTimeOffset? nowUtc = null)
    {
        EnsureNotFinal();

        if (Status != TripStatus.Requested)
            throw new InvalidOperationException("Trip must be Requested before it can be Accepted.");

        if (driverId == Guid.Empty)
            throw new ArgumentException("DriverId is required.", nameof(driverId));

        if (string.IsNullOrWhiteSpace(vehicleId))
            throw new ArgumentException("VehicleId is required.", nameof(vehicleId));

        DriverId = driverId;
        VehicleId = vehicleId.Trim();

        Status = TripStatus.Accepted;
        Touch(nowUtc);
    }

    public void MarkArriving(DateTimeOffset? nowUtc = null)
    {
        EnsureNotFinal();

        if (Status != TripStatus.Accepted)
            throw new InvalidOperationException("Trip must be Accepted before Arriving.");

        Status = TripStatus.Arriving;
        Touch(nowUtc);
    }

    public void Start(DateTimeOffset? nowUtc = null)
    {
        EnsureNotFinal();

        if (Status is not (TripStatus.Accepted or TripStatus.Arriving))
            throw new InvalidOperationException("Trip must be Accepted/Arriving before InProgress.");

        Status = TripStatus.InProgress;
        Touch(nowUtc);
    }

    public void Complete(DateTimeOffset? nowUtc = null)
    {
        EnsureNotFinal();

        if (Status != TripStatus.InProgress)
            throw new InvalidOperationException("Trip must be InProgress before Completed.");

        Status = TripStatus.Completed;
        Touch(nowUtc);
    }

    public void CancelByRider(string? reason = null, DateTimeOffset? nowUtc = null)
    {
        EnsureNotFinal();

        if (Status is TripStatus.InProgress or TripStatus.Completed)
            throw new InvalidOperationException("Cannot cancel after trip has started/completed.");

        Status = TripStatus.CanceledByRider;
        Touch(nowUtc);
    }

    public void CancelByDriver(string? reason = null, DateTimeOffset? nowUtc = null)
    {
        EnsureNotFinal();

        if (Status is TripStatus.InProgress or TripStatus.Completed)
            throw new InvalidOperationException("Cannot cancel after trip has started/completed.");

        Status = TripStatus.CanceledByDriver;
        Touch(nowUtc);
    }

    private void EnsureNotFinal()
    {
        if (Status is TripStatus.Completed or TripStatus.CanceledByRider or TripStatus.CanceledByDriver)
            throw new InvalidOperationException($"Trip is final ({Status}). No further changes allowed.");
    }

    private void Touch(DateTimeOffset? nowUtc = null)
    {
        UpdatedAtUtc = nowUtc ?? DateTimeOffset.UtcNow;
    }
}
