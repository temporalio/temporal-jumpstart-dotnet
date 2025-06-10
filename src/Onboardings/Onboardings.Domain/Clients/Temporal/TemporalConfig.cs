namespace Onboardings.Domain.Clients.Temporal;

public static class Defaults
{
    internal const int CacheMaxInstances = 1000;
    internal const int CapacityMaxConcurrentWorkflowTaskExecutors = 100;
    internal const int CapacityMaxConcurrentActivityTaskExecutors = 100;
    internal const int CapacityMaxConcurrentLocalActivityExecutors = 100;
    internal const int CapacityMaxConcurrentWorkflowTaskPollers = 5;
    internal const int CapacityMaxConcurrentActivityTaskPollers = 5;
    // internal static double? RateLimitsMaxWorkerActivitiesPerSecond = null;
    // internal static double? RateLImitsMaxTaskQueueActivitiesPerSecond = null;
}

// ReSharper disable once ClassNeverInstantiated.Global
public record WorkerConfig
{
    public required string TaskQueue { get; init; }
    public required CapacityConfig Capacity { get; init; } = new();
    public required RateLimitsConfig RateLimits { get; init; } = new();
    public required CacheConfig Cache { get; init; } = new();
}

public record CapacityConfig(
    int MaxConcurrentWorkflowTaskPollers = Defaults.CapacityMaxConcurrentWorkflowTaskPollers,
    int MaxConcurrentWorkflowTaskExecutors = Defaults.CapacityMaxConcurrentWorkflowTaskExecutors,
    int MaxConcurrentActivityTaskPollers = Defaults.CapacityMaxConcurrentActivityTaskPollers,
    int MaxConcurrentLocalActivityExecutors = Defaults.CapacityMaxConcurrentLocalActivityExecutors,
    int MaxConcurrentActivityExecutors = Defaults.CapacityMaxConcurrentActivityTaskExecutors);

public record RateLimitsConfig(
        double? MaxWorkerActivitiesPerSecond = null,
        double? MaxTaskQueueActivitiesPerSecond = null);

public record CacheConfig(int MaxInstances = Defaults.CacheMaxInstances);

// ReSharper disable once ClassNeverInstantiated.Global
public record MtlsConfig(string KeyFile, string CertChainFile);

// ReSharper disable once ClassNeverInstantiated.Global
public record ConnectionConfig(string Namespace, string Target, MtlsConfig? Mtls = null);

public record TemporalConfig
{
    public TemporalConfig()
    {
    }
    public TemporalConfig(WorkerConfig Worker, ConnectionConfig Connection)
    {
        this.Worker = Worker;
        this.Connection = Connection;
    }

    public WorkerConfig Worker { get; init; }
    public ConnectionConfig Connection { get; init; }

    public void Deconstruct(out WorkerConfig Worker, out ConnectionConfig Connection)
    {
        Worker = this.Worker;
        Connection = this.Connection;
    }
}