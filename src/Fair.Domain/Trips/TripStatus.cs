namespace Fair.Domain.Trips;

public enum TripStatus
{
    Draft = 0,
    Quoted = 1,
    Requested = 2,
    Accepted = 3,
    Arriving = 4,
    InProgress = 5,
    Completed = 6,
    CanceledByRider = 7,
    CanceledByDriver = 8,
    Expired = 9
}
