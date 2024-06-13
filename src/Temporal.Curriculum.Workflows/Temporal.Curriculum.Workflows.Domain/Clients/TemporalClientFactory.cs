using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Temporalio.Client;

namespace Temporal.Curriculum.Workflows.Domain.Clients;

public interface ITemporalClientFactory
{
    TemporalConfig GetConfig();
    Task<TemporalClient> CreateClientAsync();
}

public class TemporalClientFactory(IOptions<TemporalConfig> cfg, ILoggerFactory loggerFactory) : ITemporalClientFactory
{
    public TemporalConfig GetConfig()
    {
        return cfg.Value;
    }
    public async Task<TemporalClient> CreateClientAsync()
    {
        if (cfg.Value.Connection == null)
        {
            throw new Exception("missing Temporal config");
        }
        var logger = loggerFactory.CreateLogger<TemporalClient>();
        logger.LogInformation("connecting to temporal namespace {_cfg.Connection.Namespace}", cfg.Value.Connection.Namespace);
        var opts = new TemporalClientConnectOptions
        {
            Namespace = cfg.Value.Connection.Namespace,
            TargetHost = cfg.Value.Connection.Target,
            LoggerFactory = loggerFactory,
        };
        if (cfg.Value.Connection.Mtls != null)
        {
            logger.LogInformation("using cert from {_cfg.Connection.Mtls.CertChainFile}", cfg.Value.Connection.Mtls.CertChainFile);

            opts.Tls = new TlsOptions
            {
                ClientCert =  File.ReadAllBytes(cfg.Value.Connection.Mtls.CertChainFile),
                ClientPrivateKey =  File.ReadAllBytes(cfg.Value.Connection.Mtls.KeyFile),
            };
        }
        else
        {
            logger.LogWarning("connections to Temporal are not via mTLS");
        }
                
        return await TemporalClient.ConnectAsync(opts);
    }
}