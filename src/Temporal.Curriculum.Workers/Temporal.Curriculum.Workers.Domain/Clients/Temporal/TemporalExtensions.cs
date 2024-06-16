using System.Diagnostics;
using Temporalio.Client;
using Temporalio.Extensions.Hosting;
using Temporalio.Worker;

namespace Temporal.Curriculum.Workers.Domain.Clients.Temporal;

public static class TemporalExtensions
{
    public static TemporalClientConnectOptions ConfigureClient(this TemporalClientConnectOptions? opts,
        TemporalConfig cfg)
    {
        if (opts == null) opts = new TemporalClientConnectOptions();
        Debug.Assert(cfg.Connection != null, "Connection is required");
        opts.Namespace = cfg.Connection.Namespace;
        opts.TargetHost = cfg.Connection.Target;

        if (cfg.Connection.Mtls != null)
            opts.Tls = new TlsOptions
            {
                ClientCert = File.ReadAllBytes(cfg.Connection.Mtls.CertChainFile),
                ClientPrivateKey = File.ReadAllBytes(cfg.Connection.Mtls.KeyFile)
            };

        return opts;
    }

    public static TemporalWorkerOptions ConfigureWorker(this TemporalWorkerOptions opts, TemporalConfig cfg)
    {
        opts.UseWorkerVersioning = false;
        // rate limits
        opts.MaxTaskQueueActivitiesPerSecond = cfg.Worker.RateLimits.MaxTaskQueueActivitiesPerSecond;
        opts.MaxActivitiesPerSecond = cfg.Worker.RateLimits.MaxWorkerActivitiesPerSecond;

        // executors
        opts.MaxConcurrentActivities = cfg.Worker.Capacity.MaxConcurrentActivityExecutors;
        opts.MaxConcurrentLocalActivities = cfg.Worker.Capacity.MaxConcurrentLocalActivityExecutors;
        opts.MaxConcurrentWorkflowTasks = cfg.Worker.Capacity.MaxConcurrentWorkflowTaskExecutors;

        // pollers
        opts.MaxConcurrentActivityTaskPolls = cfg.Worker.Capacity.MaxConcurrentWorkflowTaskPollers;
        opts.MaxConcurrentActivityTaskPolls = cfg.Worker.Capacity.MaxConcurrentActivityTaskPollers;

        opts.MaxCachedWorkflows = cfg.Worker.Cache.MaxInstances;

        return opts;
    }

    public static TemporalWorkerServiceOptions ConfigureService(this TemporalWorkerServiceOptions opts,
        TemporalConfig cfg)
    {
        opts.ClientOptions = opts.ClientOptions.ConfigureClient(cfg);
        opts.ConfigureWorker(cfg);
        return opts;
    }
}