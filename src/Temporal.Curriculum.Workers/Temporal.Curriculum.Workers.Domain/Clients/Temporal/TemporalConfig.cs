namespace Temporal.Curriculum.Workers.Domain.Clients.Temporal;

public static class Defaults
{
    internal const int CacheMaxInstances = 1000;
    internal const int CapacityMaxConcurrentWorkflowTaskExecutors = 100;
    internal const int CapacityMaxConcurrentActivityTaskExecutors = 100;
    internal const int CapacityMaxConcurrentLocalActivityExecutors = 100;
    internal const int CapacityMaxConcurrentWorkflowTaskPollers = 5;
    internal const int CapacityMaxConcurrentActivityTaskPollers = 5;
    internal static double? RateLimitsMaxWorkerActivitiesPerSecond = null;
    internal static double? RateLImitsMaxTaskQueueActivitiesPerSecond = null;
}

// ReSharper disable once ClassNeverInstantiated.Global
public record WorkerConfig
{
    public required string TaskQueue { get; set; }
    public required CapacityConfig Capacity { get; set; } = new CapacityConfig();
    public required RateLimitsConfig RateLimits { get; set; } = new RateLimitsConfig();
    public required CacheConfig Cache { get; set; } = new CacheConfig();
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

public record TemporalConfig(WorkerConfig Worker, ConnectionConfig Connection);