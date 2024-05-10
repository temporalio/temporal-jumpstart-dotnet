namespace Temporal.Curriculum.Starters.Config;

public class WorkerConfig
{
    public required string TaskQueue { get; set; }
}

public class MtlsConfig
{
    public string KeyFile { get; set; }
    public string CertChainFile { get; set; }
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