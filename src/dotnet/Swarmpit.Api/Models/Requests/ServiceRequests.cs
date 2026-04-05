namespace Swarmpit.Api.Models.Requests;

public class CreateServiceRequest
{
    public string ServiceName { get; set; } = "";
    public string Image { get; set; } = "";
    public string Mode { get; set; } = "replicated";
    public int Replicas { get; set; } = 1;
    public List<string>? Command { get; set; }
    public string? User { get; set; }
    public string? Dir { get; set; }
    public bool Tty { get; set; }

    public List<PortRequest>? Ports { get; set; }
    public List<NetworkRequest>? Networks { get; set; }
    public List<MountRequest>? Mounts { get; set; }
    public List<NameValueRequest>? Variables { get; set; }
    public List<NameValueRequest>? Labels { get; set; }
    public List<SecretRefRequest>? Secrets { get; set; }
    public List<ConfigRefRequest>? Configs { get; set; }
    public List<NameValueRequest>? Hosts { get; set; }
    public ResourcesRequest? Resources { get; set; }
    public DeploymentRequest? Deployment { get; set; }
    public LogdriverRequest? Logdriver { get; set; }
}

public class PortRequest
{
    public ulong ContainerPort { get; set; }
    public ulong HostPort { get; set; }
    public string Protocol { get; set; } = "tcp";
    public string Mode { get; set; } = "ingress";
}

public class NetworkRequest
{
    public string Id { get; set; } = "";
    public string NetworkName { get; set; } = "";
}

public class MountRequest
{
    public string Type { get; set; } = "volume";
    public string Source { get; set; } = "";
    public string Target { get; set; } = "";
    public bool ReadOnly { get; set; }
}

public class NameValueRequest
{
    public string Name { get; set; } = "";
    public string Value { get; set; } = "";
}

public class SecretRefRequest
{
    public string Id { get; set; } = "";
    public string SecretName { get; set; } = "";
    public string SecretTarget { get; set; } = "";
}

public class ConfigRefRequest
{
    public string Id { get; set; } = "";
    public string ConfigName { get; set; } = "";
    public string ConfigTarget { get; set; } = "";
}

public class ResourcesRequest
{
    public ResourceConfigRequest? Reservation { get; set; }
    public ResourceConfigRequest? Limit { get; set; }
}

public class ResourceConfigRequest
{
    public double Cpu { get; set; }
    public double Memory { get; set; }
}

public class DeploymentRequest
{
    public DeploymentConfigRequest? Update { get; set; }
    public DeploymentConfigRequest? Rollback { get; set; }
    public RestartPolicyRequest? RestartPolicy { get; set; }
    public PlacementRequest? Placement { get; set; }
    public bool AutoRedeploy { get; set; }
}

public class DeploymentConfigRequest
{
    public long Parallelism { get; set; } = 1;
    public double Delay { get; set; }
    public string FailureAction { get; set; } = "pause";
    public double Monitor { get; set; }
    public string Order { get; set; } = "stop-first";
}

public class RestartPolicyRequest
{
    public string Condition { get; set; } = "any";
    public double Delay { get; set; } = 5;
    public long MaxAttempts { get; set; }
    public double Window { get; set; }
}

public class PlacementRequest
{
    public List<string>? Constraints { get; set; }
}

public class LogdriverRequest
{
    public string Name { get; set; } = "";
    public List<NameValueRequest>? Opts { get; set; }
}
