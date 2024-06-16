using System.Diagnostics;
using Temporal.Curriculum.Starters.Config;
using Temporalio.Client;

namespace Temporal.Curriculum.Starters.Clients;

public static class TemporalClientExtensions
{
    public static void UseTemporalClientHttpMiddleware(this IApplicationBuilder builder)
    {
        builder.UseMiddleware<TemporalClientHttpMiddleware>();
    }

    public static void ConfigureClient(this TemporalClientConnectOptions? opts,
        TemporalConfig cfg)
    {
        opts ??= new TemporalClientConnectOptions();
        Debug.Assert(cfg.Connection != null, "Connection is required");
        opts.Namespace = cfg.Connection.Namespace;
        opts.TargetHost = cfg.Connection.Target;

        if (cfg.Connection.Mtls != null)
            opts.Tls = new TlsOptions
            {
                ClientCert = File.ReadAllBytes(cfg.Connection.Mtls.CertChainFile),
                ClientPrivateKey = File.ReadAllBytes(cfg.Connection.Mtls.KeyFile)
            };
    }
}