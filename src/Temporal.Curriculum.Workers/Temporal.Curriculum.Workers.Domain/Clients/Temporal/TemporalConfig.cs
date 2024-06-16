namespace Temporal.Curriculum.Workers.Domain.Clients.Temporal;

public static class Defaults
{
    internal const int cacheMaxInstances = 1000;
    internal const int capacityMaxConcurrentWorkflowTaskExecutors = 100;
    internal const int capacityMaxConcurrentActivityTaskExecutors = 100;
    internal const int capacityMaxConcurrentLocalActivityExecutors = 100;
    internal const int capacityMaxConcurrentWorkflowTaskPollers = 5;
    internal const int capacityMaxConcurrentActivityTaskPollers = 5;
    internal static double? rateLimitsMaxWorkerActivitiesPerSecond = null;
    internal static double? rateLImitsMaxTaskQueueActivitiesPerSecond = null;
}

public class WorkerConfig
{
    public WorkerConfig()
    {
        Capacity = CapacityConfig.WithDefaults();
        RateLimits = RateLimitsConfig.WithDefaults();
        Cache = CacheConfig.WithDefaults();
    }

    public required string TaskQueue { get; set; }
    public required CapacityConfig Capacity { get; set; }
    public required RateLimitsConfig RateLimits { get; set; }
    public required CacheConfig Cache { get; set; }
}

public class CapacityConfig
{
    public int MaxConcurrentWorkflowTaskExecutors { get; set; }
    public int MaxConcurrentLocalActivityExecutors { get; set; }
    public int MaxConcurrentActivityExecutors { get; set; }
    public int MaxConcurrentWorkflowTaskPollers { get; set; }
    public int MaxConcurrentActivityTaskPollers { get; set; }

    public static CapacityConfig WithDefaults()
    {
        return new CapacityConfig
        {
            MaxConcurrentActivityExecutors = Defaults.capacityMaxConcurrentActivityTaskExecutors,
            MaxConcurrentActivityTaskPollers = Defaults.capacityMaxConcurrentActivityTaskPollers,
            MaxConcurrentWorkflowTaskExecutors = Defaults.capacityMaxConcurrentWorkflowTaskExecutors,
            MaxConcurrentLocalActivityExecutors = Defaults.capacityMaxConcurrentLocalActivityExecutors,
            MaxConcurrentWorkflowTaskPollers = Defaults.capacityMaxConcurrentWorkflowTaskPollers
        };
    }
}

public class RateLimitsConfig
{
    public double? MaxWorkerActivitiesPerSecond { get; set; }
    public double? MaxTaskQueueActivitiesPerSecond { get; set; }

    public static RateLimitsConfig WithDefaults()
    {
        return new RateLimitsConfig
        {
            MaxWorkerActivitiesPerSecond = Defaults.rateLimitsMaxWorkerActivitiesPerSecond,
            MaxTaskQueueActivitiesPerSecond = Defaults.rateLImitsMaxTaskQueueActivitiesPerSecond
        };
    }
}

public class CacheConfig
{
    public int MaxInstances { get; set; }

    public static CacheConfig WithDefaults()
    {
        return new CacheConfig
        {
            MaxInstances = Defaults.cacheMaxInstances
        };
    }
}

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