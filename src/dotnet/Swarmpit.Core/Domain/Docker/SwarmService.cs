namespace Swarmpit.Core.Domain.Docker;

public class SwarmService
{
    public string Id { get; set; } = "";
    public long Version { get; set; }
    public string? CreatedAt { get; set; }
    public string? UpdatedAt { get; set; }
    public string ServiceName { get; set; } = "";
    public string Mode { get; set; } = "";
    public int? Replicas { get; set; }
    public string? State { get; set; }
    public string? Stack { get; set; }
    public ServiceRepository Repository { get; set; } = new();
    public List<ServicePort> Ports { get; set; } = [];
    public List<ServiceNetwork> Networks { get; set; } = [];
    public List<ServiceMount> Mounts { get; set; } = [];
    public List<NameValue> Variables { get; set; } = [];
    public List<NameValue> Labels { get; set; } = [];
    public List<NameValue> ContainerLabels { get; set; } = [];
    public List<ServiceSecretRef> Secrets { get; set; } = [];
    public List<ServiceConfigRef> Configs { get; set; } = [];
    public List<NameValue> Hosts { get; set; } = [];
    public List<string> Command { get; set; } = [];
    public string? User { get; set; }
    public string? Dir { get; set; }
    public bool Tty { get; set; }
    public ServiceResources Resources { get; set; } = new();
    public ServiceDeployment Deployment { get; set; } = new();
    public ServiceLogdriver Logdriver { get; set; } = new();
    public ServiceHealthcheck Healthcheck { get; set; } = new();
    public List<NameValue> Links { get; set; } = [];
    public bool Immutable { get; set; }
    public bool Agent { get; set; }
    public ServiceStatus Status { get; set; } = new();

    public void ComputeStatus(int runningTaskCount, int totalDesiredCount)
    {
        Status.Tasks = new ServiceTaskStatus
        {
            Running = runningTaskCount,
            Total = totalDesiredCount
        };

        if (runningTaskCount == totalDesiredCount && totalDesiredCount > 0)
            State = "running";
        else if (runningTaskCount == 0)
            State = "not running";
        else
            State = "partly running";
    }
}

public class ServiceRepository
{
    public string Name { get; set; } = "";
    public string Tag { get; set; } = "";
    public string Image { get; set; } = "";
    public string? ImageDigest { get; set; }
}

public class ServicePort
{
    public ulong ContainerPort { get; set; }
    public string Protocol { get; set; } = "";
    public string Mode { get; set; } = "";
    public ulong HostPort { get; set; }
}

public class ServiceNetwork
{
    public string Id { get; set; } = "";
    public string NetworkName { get; set; } = "";
    public List<string> ServiceAliases { get; set; } = [];
}

public class ServiceMount
{
    public string Type { get; set; } = "";
    public string Source { get; set; } = "";
    public string Target { get; set; } = "";
    public bool ReadOnly { get; set; }
}

public class ServiceSecretRef
{
    public string Id { get; set; } = "";
    public string SecretName { get; set; } = "";
    public string SecretTarget { get; set; } = "";
}

public class ServiceConfigRef
{
    public string Id { get; set; } = "";
    public string ConfigName { get; set; } = "";
    public string ConfigTarget { get; set; } = "";
}

public class ServiceResources
{
    public ServiceResourceConfig Reservation { get; set; } = new();
    public ServiceResourceConfig Limit { get; set; } = new();
}

public class ServiceResourceConfig
{
    public double Cpu { get; set; }
    public double Memory { get; set; }
}

public class ServiceDeployment
{
    public ServiceDeploymentConfig Update { get; set; } = new();
    public ServiceDeploymentConfig Rollback { get; set; } = new();
    public ServiceRestartPolicy RestartPolicy { get; set; } = new();
    public long ForceUpdate { get; set; }
    public ServicePlacement Placement { get; set; } = new();
    public bool AutoRedeploy { get; set; }
    public bool RollbackAllowed { get; set; }
}

public class ServiceDeploymentConfig
{
    public long Parallelism { get; set; }
    public double Delay { get; set; }
    public string FailureAction { get; set; } = "";
    public double Monitor { get; set; }
    public string Order { get; set; } = "";
}

public class ServiceRestartPolicy
{
    public string Condition { get; set; } = "";
    public double Delay { get; set; }
    public long MaxAttempts { get; set; }
    public double Window { get; set; }
}

public class ServicePlacement
{
    public List<string> Constraints { get; set; } = [];
}

public class ServiceLogdriver
{
    public string Name { get; set; } = "";
    public List<NameValue> Opts { get; set; } = [];
}

public class ServiceHealthcheck
{
    public List<string> Test { get; set; } = [];
    public long Interval { get; set; }
    public long Timeout { get; set; }
    public long Retries { get; set; }
}

public class ServiceStatus
{
    public ServiceTaskStatus Tasks { get; set; } = new();
    public string? Update { get; set; }
    public string? Message { get; set; }
}

public class ServiceTaskStatus
{
    public int Running { get; set; }
    public int Total { get; set; }
}
