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
public class WorkerConfig
{
    public required string TaskQueue { get; set; }
    public required CapacityConfig Capacity { get; set; } = CapacityConfig.WithDefaults();
    public required RateLimitsConfig RateLimits { get; set; } = RateLimitsConfig.WithDefaults();
    public required CacheConfig Cache { get; set; } = CacheConfig.WithDefaults();
}

public class CapacityConfig
{
    public int MaxConcurrentWorkflowTaskExecutors { get; set; }
    public int MaxConcurrentLocalActivityExecutors { get; set; }
    public int MaxConcurrentActivityExecutors { get; set; }
    public int MaxConcurrentWorkflowTaskPollers { get; set; }
    public int MaxConcurrentActivityTaskPollers { get; set; }

    public static CapacityConfig WithDefaults() =>
        new() {
            MaxConcurrentActivityExecutors = Defaults.CapacityMaxConcurrentActivityTaskExecutors,
            MaxConcurrentActivityTaskPollers = Defaults.CapacityMaxConcurrentActivityTaskPollers,
            MaxConcurrentWorkflowTaskExecutors = Defaults.CapacityMaxConcurrentWorkflowTaskExecutors,
            MaxConcurrentLocalActivityExecutors = Defaults.CapacityMaxConcurrentLocalActivityExecutors,
            MaxConcurrentWorkflowTaskPollers = Defaults.CapacityMaxConcurrentWorkflowTaskPollers
        };
}

public class RateLimitsConfig
{
    public double? MaxWorkerActivitiesPerSecond { get; set; }
    public double? MaxTaskQueueActivitiesPerSecond { get; set; }

    public static RateLimitsConfig WithDefaults() =>
        new() {
            MaxWorkerActivitiesPerSecond = Defaults.RateLimitsMaxWorkerActivitiesPerSecond,
            MaxTaskQueueActivitiesPerSecond = Defaults.RateLImitsMaxTaskQueueActivitiesPerSecond
        };
}

public class CacheConfig
{
    public int MaxInstances { get; set; }

    public static CacheConfig WithDefaults() =>
        new() { MaxInstances = Defaults.CacheMaxInstances };
}

// ReSharper disable once ClassNeverInstantiated.Global
public class MtlsConfig
{
    public required string KeyFile { get; set; }
    public required string CertChainFile { get; set; }
}

public class ConnectionConfig
{
    public required string Namespace { get; set; }
    public required string Target { get; set; }

    public MtlsConfig? Mtls { get; set; }
}

public class TemporalConfig
{
    public required WorkerConfig Worker { get; set; }
    public required ConnectionConfig Connection { get; set; }
}