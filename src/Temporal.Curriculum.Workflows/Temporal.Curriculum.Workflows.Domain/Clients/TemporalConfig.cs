namespace Temporal.Curriculum.Workflows.Domain.Clients;

public class WorkerConfig
{
    public required string TaskQueue { get; set; }
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