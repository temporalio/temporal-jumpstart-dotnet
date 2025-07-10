using System.Diagnostics;
using Temporalio.Client;
using Temporalio.Extensions.Hosting;
using Temporalio.Runtime;
using Temporalio.Worker;

namespace Onboardings.Domain.Clients.Temporal;

public static class TemporalExtensions
{
    public static TemporalClientConnectOptions ConfigureClient(this TemporalClientConnectOptions? opts,
        TemporalConfig cfg)
    {
        opts ??= new TemporalClientConnectOptions();
        
        Debug.Assert(cfg.Connection != null, "Connection is required");
        
        TemporalRuntimeOptions? runtimeOpts = null;
        
        if (cfg.Metrics != null && cfg.Metrics.Prometheus != null && cfg.Metrics.Prometheus.BindAddress != null)
        {
            runtimeOpts = new TemporalRuntimeOptions();
            runtimeOpts.Telemetry = new TelemetryOptions
            {
                Metrics = new MetricsOptions
                {
                    Prometheus = new PrometheusOptions(cfg.Metrics.Prometheus.BindAddress),
                }
            };
        }

        if (runtimeOpts != null)
        {
            opts.Runtime = new TemporalRuntime(runtimeOpts);
        }
        
        opts.Namespace = cfg.Connection.Namespace;
        opts.TargetHost = cfg.Connection.Target;

        if (cfg.Connection.Mtls != null)
        {
            opts.Tls = new TlsOptions {
                ClientCert = File.ReadAllBytes(cfg.Connection.Mtls.CertChainFile),
                ClientPrivateKey = File.ReadAllBytes(cfg.Connection.Mtls.KeyFile)
            };
        }

        return opts;
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public static void ConfigureWorker(this TemporalWorkerOptions opts, TemporalConfig cfg)
    {
        // rate limits
        opts.MaxTaskQueueActivitiesPerSecond = cfg.Worker.RateLimits.MaxTaskQueueActivitiesPerSecond;
        opts.MaxActivitiesPerSecond = cfg.Worker.RateLimits.MaxWorkerActivitiesPerSecond;

        // executors
        opts.MaxConcurrentActivities = cfg.Worker.Capacity.MaxConcurrentActivityExecutors;
        opts.MaxConcurrentLocalActivities = cfg.Worker.Capacity.MaxConcurrentLocalActivityExecutors;
        opts.MaxConcurrentWorkflowTasks = cfg.Worker.Capacity.MaxConcurrentWorkflowTaskExecutors;

        // pollers
        opts.MaxConcurrentWorkflowTaskPolls = cfg.Worker.Capacity.MaxConcurrentWorkflowTaskPollers;
        opts.MaxConcurrentActivityTaskPolls = cfg.Worker.Capacity.MaxConcurrentActivityTaskPollers;

        opts.MaxCachedWorkflows = cfg.Worker.Cache.MaxInstances;
    }

    public static void ConfigureService(this TemporalWorkerServiceOptions opts,
        TemporalConfig cfg)
    {
        opts.ClientOptions = opts.ClientOptions.ConfigureClient(cfg);
        opts.ConfigureWorker(cfg);
    }
}