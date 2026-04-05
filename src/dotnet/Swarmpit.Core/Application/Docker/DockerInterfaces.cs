using Swarmpit.Core.Domain.Docker;

namespace Swarmpit.Core.Application.Docker;

public interface IServiceRepository
{
    Task<List<SwarmService>> ListAsync();
    Task<SwarmService?> GetAsync(string id);
    Task<string> CreateAsync(CreateServiceParams request);
    Task UpdateAsync(string id, CreateServiceParams request);
    Task DeleteAsync(string id);
    Task RedeployAsync(string id, string? tag = null);
    Task RollbackAsync(string id);
    Task StopAsync(string id);
    Task<List<object>> GetLogsAsync(string id, string? since = null);
    Task<string> GetComposeYamlAsync(string id);
    Task<List<SwarmTask>> GetTasksAsync(string id);
    Task<List<SwarmNetwork>> GetNetworksAsync(string id);
    Task<List<SwarmService>> GetBySecretAsync(string secretId);
    Task<List<SwarmService>> GetByConfigAsync(string configId);
    Task<List<SwarmService>> GetByVolumeAsync(string volumeName);
    Task<List<SwarmService>> GetByNetworkAsync(string networkId);
    Task<List<SwarmService>> GetByStackAsync(string stackName);
}

public interface INodeRepository
{
    Task<List<SwarmNode>> ListAsync();
    Task<SwarmNode?> GetAsync(string id);
    Task<List<SwarmTask>> GetTasksAsync(string id);
    Task<int> GetActiveCountAsync();
}

public interface INetworkRepository
{
    Task<List<SwarmNetwork>> ListAsync();
    Task<SwarmNetwork?> GetAsync(string id);
    Task<string> CreateAsync(CreateNetworkParams request);
    Task DeleteAsync(string id);
    Task<List<SwarmNetwork>> GetByStackAsync(string stackName);
}

public interface IVolumeRepository
{
    Task<List<SwarmVolume>> ListAsync();
    Task<SwarmVolume?> GetAsync(string name);
    Task<SwarmVolume> CreateAsync(CreateVolumeParams request);
    Task DeleteAsync(string name);
    Task<List<SwarmVolume>> GetByStackAsync(string stackName);
}

public interface ISecretRepository
{
    Task<List<SwarmSecret>> ListAsync();
    Task<SwarmSecret?> GetAsync(string id);
    Task<string> CreateAsync(string secretName, string data);
    Task DeleteAsync(string id);
    Task<List<SwarmSecret>> GetByStackAsync(string stackName);
}

public interface IConfigRepository
{
    Task<List<SwarmConfig>> ListAsync();
    Task<SwarmConfig?> GetAsync(string id);
    Task<string> CreateAsync(string configName, string data);
    Task DeleteAsync(string id);
    Task<List<SwarmConfig>> GetByStackAsync(string stackName);
}

public interface ITaskRepository
{
    Task<List<SwarmTask>> ListAsync();
    Task<SwarmTask?> GetAsync(string id);
}

public interface IContainerRepository
{
    Task<List<Container>> ListAsync(bool all = true);
    Task<ContainerDetail?> GetAsync(string id);
    Task StartAsync(string id);
    Task StopAsync(string id);
    Task RestartAsync(string id);
    Task RemoveAsync(string id, bool force = false);
    Task<List<object>> GetLogsAsync(string id, string? since = null, int tail = 500);
}

public class CreateServiceParams
{
    public string ServiceName { get; set; } = "";
    public string Image { get; set; } = "";
    public string Mode { get; set; } = "replicated";
    public int Replicas { get; set; } = 1;
    public List<string>? Command { get; set; }
    public string? User { get; set; }
    public string? Dir { get; set; }
    public bool Tty { get; set; }
    public List<ServicePort>? Ports { get; set; }
    public List<ServiceNetwork>? Networks { get; set; }
    public List<ServiceMount>? Mounts { get; set; }
    public List<NameValue>? Variables { get; set; }
    public List<NameValue>? Labels { get; set; }
    public List<ServiceSecretRef>? Secrets { get; set; }
    public List<ServiceConfigRef>? Configs { get; set; }
    public List<NameValue>? Hosts { get; set; }
    public ServiceResources? Resources { get; set; }
    public ServiceDeployment? Deployment { get; set; }
    public ServiceLogdriver? Logdriver { get; set; }
}

public class CreateNetworkParams
{
    public string NetworkName { get; set; } = "";
    public string? Driver { get; set; }
    public bool Internal { get; set; }
    public bool Attachable { get; set; }
    public bool Ingress { get; set; }
    public bool EnableIPv6 { get; set; }
    public NetworkIpam? Ipam { get; set; }
    public Dictionary<string, string>? Options { get; set; }
}

public class CreateVolumeParams
{
    public string VolumeName { get; set; } = "";
    public string? Driver { get; set; }
    public Dictionary<string, string>? Options { get; set; }
    public Dictionary<string, string>? Labels { get; set; }
}
