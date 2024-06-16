using System.Diagnostics;
using Temporal.Curriculum.Starters.Config;
using Temporalio.Client;

namespace Temporal.Curriculum.Starters.Clients;

public static class TemporalClientExtensions
{
    public static IApplicationBuilder UseTemporalClientHttpMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TemporalClientHttpMiddleware>();
    }
    public static TemporalClientConnectOptions ConfigureClient(this TemporalClientConnectOptions? opts,
        TemporalConfig cfg)
    {
        
        if(opts == null)
        {
            opts = new TemporalClientConnectOptions();
        }
        Debug.Assert(cfg.Connection != null, "Connection is required");
        opts.Namespace = cfg.Connection.Namespace;
        opts.TargetHost = cfg.Connection.Target;
        
        if (cfg.Connection.Mtls != null)
        {
            opts.Tls = new TlsOptions
            {
                ClientCert =  File.ReadAllBytes(cfg.Connection.Mtls.CertChainFile),
                ClientPrivateKey =  File.ReadAllBytes(cfg.Connection.Mtls.KeyFile),
            };
        }

        return opts;
    }
}