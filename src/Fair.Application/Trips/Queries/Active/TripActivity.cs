namespace Fair.Application.Trips.Queries.Active;

public static class TripActivity
{
    // Single source of truth: "active" statuses
    private static readonly HashSet<string> ActiveStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Requested",
        "Accepted",
        "Arriving",
        "InProgress"
    };

    public static bool IsActive(string status) => ActiveStatuses.Contains(status);
}