namespace Fair.Infrastructure.Dispatch;

public sealed class DispatchWorkerOptions
{
    // Hur ofta worker ska köra expire
    public int ExpireSweepSeconds { get; init; } = 5;
}