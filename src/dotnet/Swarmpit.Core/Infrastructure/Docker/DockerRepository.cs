using Docker.DotNet;
using Docker.DotNet.Models;
using Swarmpit.Core.Application.Docker;
using Swarmpit.Core.Domain;
using Swarmpit.Core.Domain.Docker;
using Swarmpit.Core.Infrastructure.Docker.Mappers;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Swarmpit.Core.Infrastructure.Docker;

public class DockerRepository(DockerClientFactory docker) :
    IServiceRepository, INodeRepository, INetworkRepository,
    IVolumeRepository, ISecretRepository, IConfigRepository, ITaskRepository
{
    // ──── Services ────

    async Task<List<Domain.Docker.SwarmService>> IServiceRepository.ListAsync()
    {
        var client = docker.GetClient();

        var services = await client.Swarm.ListServicesAsync();
        var networks = await client.Networks.ListNetworksAsync();
        var tasks = await client.Tasks.ListAsync();
        var nodes = await client.Swarm.ListNodesAsync();

        var tasksByService = tasks
            .GroupBy(t => t.ServiceID ?? "")
            .ToDictionary(g => g.Key, g => g.ToList());

        var nodeCount = CountActiveNodes(nodes);

        return services.Select(svc =>
        {
            var service = ServiceMapper.ToSwarmService(svc, networks);
            var serviceTasks = tasksByService.GetValueOrDefault(svc.ID ?? "", []);
            ComputeServiceStatus(service, serviceTasks, nodeCount);
            return service;
        }).ToList();
    }

    async Task<Domain.Docker.SwarmService?> IServiceRepository.GetAsync(string id)
    {
        var client = docker.GetClient();

        global::Docker.DotNet.Models.SwarmService svc;
        try
        {
            svc = await client.Swarm.InspectServiceAsync(id);
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        var actualId = svc.ID;
        var networks = await client.Networks.ListNetworksAsync();
        var tasks = await client.Tasks.ListAsync();
        var nodes = await client.Swarm.ListNodesAsync();

        var serviceTasks = tasks.Where(t => t.ServiceID == actualId).ToList();
        var nodeCount = CountActiveNodes(nodes);

        var service = ServiceMapper.ToSwarmService(svc, networks);
        ComputeServiceStatus(service, serviceTasks, nodeCount);

        return service;
    }

    async Task<string> IServiceRepository.CreateAsync(CreateServiceParams request)
    {
        var client = docker.GetClient();
        var spec = await BuildServiceSpec(client, request);

        var response = await client.Swarm.CreateServiceAsync(new ServiceCreateParameters
        {
            Service = spec
        });

        return response.ID;
    }

    async Task IServiceRepository.UpdateAsync(string id, CreateServiceParams request)
    {
        var client = docker.GetClient();

        var svc = await client.Swarm.InspectServiceAsync(id);
        var version = svc.Version?.Index ?? 0;
        var spec = await BuildServiceSpec(client, request);

        await client.Swarm.UpdateServiceAsync(id, new ServiceUpdateParameters
        {
            Service = spec,
            Version = (long)version
        });
    }

    async Task IServiceRepository.DeleteAsync(string id)
    {
        var client = docker.GetClient();
        await client.Swarm.RemoveServiceAsync(id);
    }

    async Task IServiceRepository.RedeployAsync(string id, string? tag)
    {
        var client = docker.GetClient();
        var svc = await client.Swarm.InspectServiceAsync(id);

        var version = svc.Version?.Index ?? 0;
        var spec = svc.Spec;

        if (spec?.TaskTemplate?.ContainerSpec != null)
        {
            spec.TaskTemplate.ForceUpdate = spec.TaskTemplate.ForceUpdate + 1;

            if (!string.IsNullOrEmpty(tag))
            {
                var currentImage = spec.TaskTemplate.ContainerSpec.Image ?? "";
                var atIndex = currentImage.IndexOf('@');
                if (atIndex >= 0)
                    currentImage = currentImage[..atIndex];

                var colonIndex = currentImage.LastIndexOf(':');
                var imageName = colonIndex >= 0 ? currentImage[..colonIndex] : currentImage;
                spec.TaskTemplate.ContainerSpec.Image = $"{imageName}:{tag}";
            }
        }

        await client.Swarm.UpdateServiceAsync(id, new ServiceUpdateParameters
        {
            Service = spec!,
            Version = (long)version
        });
    }

    async Task IServiceRepository.RollbackAsync(string id)
    {
        var client = docker.GetClient();
        var svc = await client.Swarm.InspectServiceAsync(id);

        if (svc.PreviousSpec == null)
            throw new InvalidOperationException("No previous spec available for rollback");

        var version = svc.Version?.Index ?? 0;

        await client.Swarm.UpdateServiceAsync(id, new ServiceUpdateParameters
        {
            Service = svc.PreviousSpec,
            Version = (long)version
        });
    }

    async Task IServiceRepository.StopAsync(string id)
    {
        var client = docker.GetClient();
        var svc = await client.Swarm.InspectServiceAsync(id);

        if (svc.Spec?.Mode?.Replicated == null)
            throw new InvalidOperationException("Stop is only supported for replicated services");

        var version = svc.Version?.Index ?? 0;
        svc.Spec.Mode.Replicated.Replicas = 0;

        await client.Swarm.UpdateServiceAsync(id, new ServiceUpdateParameters
        {
            Service = svc.Spec,
            Version = (long)version
        });
    }

    async Task<List<object>> IServiceRepository.GetLogsAsync(string id, string? since)
    {
        var client = docker.GetClient();

        // Verify service exists
        try
        {
            await client.Swarm.InspectServiceAsync(id);
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new KeyNotFoundException($"Service {id} not found");
        }

        var sinceParam = ParseSinceParameter(since);

        try
        {
            var logsStream = await client.Swarm.GetServiceLogsAsync(
                id,
                new ServiceLogsParameters
                {
                    ShowStdout = true,
                    ShowStderr = true,
                    Tail = "500",
                    Timestamps = true,
                    Since = sinceParam
                },
                CancellationToken.None);

            var logLines = new List<object>();
            using var reader = new StreamReader(logsStream);

            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (string.IsNullOrEmpty(line)) continue;

                var timestamp = "";
                var content = line;

                var spaceIdx = line.IndexOf(' ');
                if (spaceIdx > 0 && line.Length > spaceIdx + 1)
                {
                    var possibleTimestamp = line[..spaceIdx];
                    if (possibleTimestamp.Contains('T') && possibleTimestamp.Contains(':'))
                    {
                        timestamp = possibleTimestamp;
                        content = line[(spaceIdx + 1)..];
                    }
                }

                logLines.Add(new { line = content, timestamp });
            }

            return logLines;
        }
        catch
        {
            return [];
        }
    }

    async Task<string> IServiceRepository.GetComposeYamlAsync(string id)
    {
        var client = docker.GetClient();
        var svc = await client.Swarm.InspectServiceAsync(id);

        var spec = svc.Spec;
        var containerSpec = spec?.TaskTemplate?.ContainerSpec;
        var serviceName = spec?.Name ?? id;

        var serviceConfig = new Dictionary<string, object>();

        var image = containerSpec?.Image ?? "";
        var atIdx = image.IndexOf('@');
        if (atIdx >= 0) image = image[..atIdx];
        serviceConfig["image"] = image;

        var ports = svc.Endpoint?.Ports;
        if (ports is { Count: > 0 })
        {
            serviceConfig["ports"] = ports.Select(p =>
                $"{p.PublishedPort}:{p.TargetPort}/{p.Protocol ?? "tcp"}").ToList();
        }

        var taskNetworks = spec?.TaskTemplate?.Networks;
        if (taskNetworks is { Count: > 0 })
        {
            serviceConfig["networks"] = taskNetworks
                .Select(n => n.Target ?? "")
                .Where(n => !string.IsNullOrEmpty(n))
                .ToList();
        }

        var mounts = containerSpec?.Mounts;
        if (mounts is { Count: > 0 })
        {
            serviceConfig["volumes"] = mounts.Select(m =>
            {
                var ro = m.ReadOnly ? ":ro" : "";
                return $"{m.Source}:{m.Target}{ro}";
            }).ToList();
        }

        var env = containerSpec?.Env;
        if (env is { Count: > 0 })
        {
            serviceConfig["environment"] = env.ToList();
        }

        var deploy = new Dictionary<string, object>();
        var replicated = spec?.Mode?.Replicated;
        if (replicated != null)
        {
            deploy["replicas"] = (int)(replicated.Replicas ?? 1);
        }
        else if (spec?.Mode?.Global != null)
        {
            deploy["mode"] = "global";
        }

        var resources = spec?.TaskTemplate?.Resources;
        if (resources != null)
        {
            var resConfig = new Dictionary<string, object>();

            if (resources.Limits != null)
            {
                var limits = new Dictionary<string, object>();
                if (resources.Limits.NanoCPUs > 0)
                    limits["cpus"] = $"{resources.Limits.NanoCPUs / 1_000_000_000.0:F2}";
                if (resources.Limits.MemoryBytes > 0)
                    limits["memory"] = $"{resources.Limits.MemoryBytes / (1024 * 1024)}M";
                if (limits.Count > 0) resConfig["limits"] = limits;
            }

            if (resources.Reservations != null)
            {
                var reservations = new Dictionary<string, object>();
                if (resources.Reservations.NanoCPUs > 0)
                    reservations["cpus"] = $"{resources.Reservations.NanoCPUs / 1_000_000_000.0:F2}";
                if (resources.Reservations.MemoryBytes > 0)
                    reservations["memory"] = $"{resources.Reservations.MemoryBytes / (1024 * 1024)}M";
                if (reservations.Count > 0) resConfig["reservations"] = reservations;
            }

            if (resConfig.Count > 0)
                deploy["resources"] = resConfig;
        }

        var placement = spec?.TaskTemplate?.Placement;
        if (placement?.Constraints is { Count: > 0 })
        {
            deploy["placement"] = new Dictionary<string, object>
            {
                ["constraints"] = placement.Constraints.ToList()
            };
        }

        if (deploy.Count > 0)
            serviceConfig["deploy"] = deploy;

        var composeDoc = new Dictionary<string, object>
        {
            ["version"] = "3.8",
            ["services"] = new Dictionary<string, object>
            {
                [serviceName] = serviceConfig
            }
        };

        var serializer = new SerializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        return serializer.Serialize(composeDoc);
    }

    async Task<List<Domain.Docker.SwarmTask>> IServiceRepository.GetTasksAsync(string id)
    {
        var client = docker.GetClient();

        // Resolve actual service ID (input could be name or ID)
        var svc = await client.Swarm.InspectServiceAsync(id);
        var actualId = svc.ID;

        var tasks = await client.Tasks.ListAsync();
        var services = await client.Swarm.ListServicesAsync();
        var nodes = await client.Swarm.ListNodesAsync();

        var serviceNames = services.ToDictionary(s => s.ID ?? "", s => s.Spec?.Name ?? "");
        var nodeNames = nodes.ToDictionary(n => n.ID ?? "", n => n.Description?.Hostname ?? "");

        return tasks
            .Where(t => t.ServiceID == actualId)
            .Select(t => TaskMapper.ToSwarmTask(t, serviceNames, nodeNames))
            .ToList();
    }

    async Task<List<Domain.Docker.SwarmNetwork>> IServiceRepository.GetNetworksAsync(string id)
    {
        var client = docker.GetClient();

        global::Docker.DotNet.Models.SwarmService svc;
        try
        {
            svc = await client.Swarm.InspectServiceAsync(id);
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new KeyNotFoundException($"Service {id} not found");
        }

        var networkIds = new List<string>();

        var taskNetworks = svc.Spec?.TaskTemplate?.Networks;
        if (taskNetworks != null)
        {
            networkIds.AddRange(taskNetworks
                .Select(n => n.Target)
                .Where(n => n != null)!);
        }

        var specNetworks = svc.Spec?.Networks;
        if (specNetworks != null)
        {
            foreach (var n in specNetworks)
            {
                if (n.Target != null && !networkIds.Contains(n.Target))
                    networkIds.Add(n.Target);
            }
        }

        var mapped = new List<Domain.Docker.SwarmNetwork>();
        foreach (var networkId in networkIds)
        {
            try
            {
                var network = await client.Networks.InspectNetworkAsync(networkId);
                mapped.Add(NetworkMapper.ToSwarmNetwork(network));
            }
            catch (DockerApiException)
            {
                // Network may have been removed; skip it
            }
        }

        return mapped;
    }

    // ──── Nodes ────

    async Task<List<Domain.Docker.SwarmNode>> INodeRepository.ListAsync()
    {
        var client = docker.GetClient();
        var nodes = await client.Swarm.ListNodesAsync();
        return nodes.Select(NodeMapper.ToSwarmNode).ToList();
    }

    async Task<Domain.Docker.SwarmNode?> INodeRepository.GetAsync(string id)
    {
        var client = docker.GetClient();
        try
        {
            var node = await client.Swarm.InspectNodeAsync(id);
            return NodeMapper.ToSwarmNode(node);
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    async Task<List<Domain.Docker.SwarmTask>> INodeRepository.GetTasksAsync(string id)
    {
        var client = docker.GetClient();
        var tasks = await client.Tasks.ListAsync();
        var services = await client.Swarm.ListServicesAsync();
        var nodes = await client.Swarm.ListNodesAsync();

        var serviceNames = services.ToDictionary(s => s.ID ?? "", s => s.Spec?.Name ?? "");
        var nodeNames = nodes.ToDictionary(n => n.ID ?? "", n => n.Description?.Hostname ?? "");

        return tasks
            .Where(t => t.NodeID == id)
            .Select(t => TaskMapper.ToSwarmTask(t, serviceNames, nodeNames))
            .ToList();
    }

    async Task<int> INodeRepository.GetActiveCountAsync()
    {
        var client = docker.GetClient();
        var nodes = await client.Swarm.ListNodesAsync();
        return CountActiveNodes(nodes);
    }

    // ──── Networks ────

    async Task<List<Domain.Docker.SwarmNetwork>> INetworkRepository.ListAsync()
    {
        var client = docker.GetClient();
        var networks = await client.Networks.ListNetworksAsync();
        return networks.Select(NetworkMapper.ToSwarmNetwork).ToList();
    }

    async Task<Domain.Docker.SwarmNetwork?> INetworkRepository.GetAsync(string id)
    {
        var client = docker.GetClient();
        try
        {
            var network = await client.Networks.InspectNetworkAsync(id);
            return NetworkMapper.ToSwarmNetwork(network);
        }
        catch (DockerNetworkNotFoundException)
        {
            return null;
        }
    }

    async Task<string> INetworkRepository.CreateAsync(CreateNetworkParams request)
    {
        var client = docker.GetClient();

        var parameters = new NetworksCreateParameters
        {
            Name = request.NetworkName,
            Driver = request.Driver ?? "overlay",
            Internal = request.Internal,
            Attachable = request.Attachable,
            Ingress = request.Ingress,
            EnableIPv6 = request.EnableIPv6,
            Options = request.Options ?? new Dictionary<string, string>()
        };

        if (request.Ipam != null)
        {
            parameters.IPAM = new IPAM
            {
                Config = new List<IPAMConfig>
                {
                    new()
                    {
                        Subnet = request.Ipam.Subnet,
                        Gateway = request.Ipam.Gateway
                    }
                }
            };
        }

        var response = await client.Networks.CreateNetworkAsync(parameters);
        return response.ID;
    }

    async Task INetworkRepository.DeleteAsync(string id)
    {
        var client = docker.GetClient();
        await client.Networks.DeleteNetworkAsync(id);
    }

    // ──── Volumes ────

    async Task<List<Domain.Docker.SwarmVolume>> IVolumeRepository.ListAsync()
    {
        var client = docker.GetClient();
        var response = await client.Volumes.ListAsync();
        return (response.Volumes ?? [])
            .Select(VolumeMapper.ToSwarmVolume)
            .ToList();
    }

    async Task<Domain.Docker.SwarmVolume?> IVolumeRepository.GetAsync(string name)
    {
        var client = docker.GetClient();
        try
        {
            var volume = await client.Volumes.InspectAsync(name);
            return VolumeMapper.ToSwarmVolume(volume);
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    async Task<Domain.Docker.SwarmVolume> IVolumeRepository.CreateAsync(CreateVolumeParams request)
    {
        var client = docker.GetClient();

        var parameters = new VolumesCreateParameters
        {
            Name = request.VolumeName,
            Driver = request.Driver ?? "local",
            DriverOpts = request.Options ?? new Dictionary<string, string>(),
            Labels = request.Labels ?? new Dictionary<string, string>()
        };

        var volume = await client.Volumes.CreateAsync(parameters);
        return VolumeMapper.ToSwarmVolume(volume);
    }

    async Task IVolumeRepository.DeleteAsync(string name)
    {
        var client = docker.GetClient();
        await client.Volumes.RemoveAsync(name);
    }

    // ──── Secrets ────

    async Task<List<Domain.Docker.SwarmSecret>> ISecretRepository.ListAsync()
    {
        var client = docker.GetClient();
        var secrets = await client.Secrets.ListAsync();
        return secrets.Select(SecretMapper.ToSwarmSecret).ToList();
    }

    async Task<Domain.Docker.SwarmSecret?> ISecretRepository.GetAsync(string id)
    {
        var client = docker.GetClient();
        try
        {
            var secret = await client.Secrets.InspectAsync(id);
            return SecretMapper.ToSwarmSecret(secret);
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    async Task<string> ISecretRepository.CreateAsync(string secretName, string data)
    {
        var client = docker.GetClient();

        var spec = new SecretSpec
        {
            Name = secretName,
            Data = Convert.FromBase64String(data)
        };

        var response = await client.Secrets.CreateAsync(spec);
        return response.ID;
    }

    async Task ISecretRepository.DeleteAsync(string id)
    {
        var client = docker.GetClient();
        await client.Secrets.DeleteAsync(id);
    }

    // ──── Configs ────

    async Task<List<Domain.Docker.SwarmConfig>> IConfigRepository.ListAsync()
    {
        var client = docker.GetClient();
        var configs = await client.Configs.ListConfigsAsync();
        return configs.Select(ConfigMapper.ToSwarmConfig).ToList();
    }

    async Task<Domain.Docker.SwarmConfig?> IConfigRepository.GetAsync(string id)
    {
        var client = docker.GetClient();
        try
        {
            var config = await client.Configs.InspectConfigAsync(id);
            return ConfigMapper.ToSwarmConfig(config);
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    async Task<string> IConfigRepository.CreateAsync(string configName, string data)
    {
        var client = docker.GetClient();

        var body = new SwarmCreateConfigParameters
        {
            Config = new SwarmConfigSpec
            {
                Name = configName,
                Data = Convert.FromBase64String(data)
            }
        };

        var response = await client.Configs.CreateConfigAsync(body);
        return response.ID;
    }

    async Task IConfigRepository.DeleteAsync(string id)
    {
        var client = docker.GetClient();
        await client.Configs.RemoveConfigAsync(id);
    }

    // ──── Tasks ────

    async Task<List<Domain.Docker.SwarmTask>> ITaskRepository.ListAsync()
    {
        var client = docker.GetClient();

        var tasks = await client.Tasks.ListAsync();
        var services = await client.Swarm.ListServicesAsync();
        var nodes = await client.Swarm.ListNodesAsync();

        var serviceNames = services.ToDictionary(s => s.ID ?? "", s => s.Spec?.Name ?? "");
        var nodeNames = nodes.ToDictionary(n => n.ID ?? "", n => n.Description?.Hostname ?? "");

        return tasks
            .Select(t => TaskMapper.ToSwarmTask(t, serviceNames, nodeNames))
            .ToList();
    }

    async Task<Domain.Docker.SwarmTask?> ITaskRepository.GetAsync(string id)
    {
        var client = docker.GetClient();

        TaskResponse task;
        try
        {
            task = await client.Tasks.InspectAsync(id);
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        var services = await client.Swarm.ListServicesAsync();
        var nodes = await client.Swarm.ListNodesAsync();

        var serviceNames = services.ToDictionary(s => s.ID ?? "", s => s.Spec?.Name ?? "");
        var nodeNames = nodes.ToDictionary(n => n.ID ?? "", n => n.Description?.Hostname ?? "");

        return TaskMapper.ToSwarmTask(task, serviceNames, nodeNames);
    }

    // ──── Cross-entity queries ────

    async Task<List<Domain.Docker.SwarmService>> IServiceRepository.GetBySecretAsync(string secretId)
    {
        var client = docker.GetClient();
        var services = await client.Swarm.ListServicesAsync();
        var networks = await client.Networks.ListNetworksAsync();

        return services
            .Where(svc =>
            {
                var secrets = svc.Spec?.TaskTemplate?.ContainerSpec?.Secrets;
                return secrets != null && secrets.Any(s => s.SecretID == secretId);
            })
            .Select(svc => ServiceMapper.ToSwarmService(svc, networks))
            .ToList();
    }

    async Task<List<Domain.Docker.SwarmService>> IServiceRepository.GetByConfigAsync(string configId)
    {
        var client = docker.GetClient();
        var services = await client.Swarm.ListServicesAsync();
        var networks = await client.Networks.ListNetworksAsync();

        return services
            .Where(svc =>
            {
                var configs = svc.Spec?.TaskTemplate?.ContainerSpec?.Configs;
                return configs != null && configs.Any(c => c.ConfigID == configId);
            })
            .Select(svc => ServiceMapper.ToSwarmService(svc, networks))
            .ToList();
    }

    async Task<List<Domain.Docker.SwarmService>> IServiceRepository.GetByVolumeAsync(string volumeName)
    {
        var client = docker.GetClient();
        var services = await client.Swarm.ListServicesAsync();
        var networks = await client.Networks.ListNetworksAsync();

        return services
            .Where(svc =>
            {
                var mounts = svc.Spec?.TaskTemplate?.ContainerSpec?.Mounts;
                return mounts != null && mounts.Any(m => m.Source == volumeName);
            })
            .Select(svc => ServiceMapper.ToSwarmService(svc, networks))
            .ToList();
    }

    async Task<List<Domain.Docker.SwarmService>> IServiceRepository.GetByNetworkAsync(string networkId)
    {
        var client = docker.GetClient();
        var services = await client.Swarm.ListServicesAsync();
        var networks = await client.Networks.ListNetworksAsync();

        return services
            .Where(svc =>
            {
                var taskNetworks = svc.Spec?.TaskTemplate?.Networks;
                if (taskNetworks != null && taskNetworks.Any(n => n.Target == networkId))
                    return true;

                var specNetworks = svc.Spec?.Networks;
                if (specNetworks != null && specNetworks.Any(n => n.Target == networkId))
                    return true;

                return false;
            })
            .Select(svc => ServiceMapper.ToSwarmService(svc, networks))
            .ToList();
    }

    async Task<List<Domain.Docker.SwarmService>> IServiceRepository.GetByStackAsync(string stackName)
    {
        var client = docker.GetClient();
        var services = await client.Swarm.ListServicesAsync();
        var networks = await client.Networks.ListNetworksAsync();
        var tasks = await client.Tasks.ListAsync();
        var nodes = await client.Swarm.ListNodesAsync();

        var tasksByService = tasks
            .GroupBy(t => t.ServiceID ?? "")
            .ToDictionary(g => g.Key, g => g.ToList());

        var nodeCount = CountActiveNodes(nodes);

        return services
            .Where(s => GetStackLabel(s.Spec?.Labels) == stackName)
            .Select(s =>
            {
                var svc = ServiceMapper.ToSwarmService(s, networks);
                var svcTasks = tasksByService.GetValueOrDefault(s.ID ?? "", []);
                ComputeServiceStatus(svc, svcTasks, nodeCount);
                return svc;
            })
            .ToList();
    }

    async Task<List<Domain.Docker.SwarmNetwork>> INetworkRepository.GetByStackAsync(string stackName)
    {
        var client = docker.GetClient();
        var networks = await client.Networks.ListNetworksAsync();

        return networks
            .Where(n => GetStackLabel(n.Labels) == stackName)
            .Select(NetworkMapper.ToSwarmNetwork)
            .ToList();
    }

    async Task<List<Domain.Docker.SwarmVolume>> IVolumeRepository.GetByStackAsync(string stackName)
    {
        var client = docker.GetClient();
        var volumes = await client.Volumes.ListAsync();

        return (volumes?.Volumes ?? [])
            .Where(v => GetStackLabel(v.Labels) == stackName)
            .Select(VolumeMapper.ToSwarmVolume)
            .ToList();
    }

    async Task<List<Domain.Docker.SwarmSecret>> ISecretRepository.GetByStackAsync(string stackName)
    {
        var client = docker.GetClient();
        var secrets = await client.Secrets.ListAsync();

        return secrets
            .Where(s => GetStackLabel(s.Spec?.Labels) == stackName)
            .Select(SecretMapper.ToSwarmSecret)
            .ToList();
    }

    async Task<List<Domain.Docker.SwarmConfig>> IConfigRepository.GetByStackAsync(string stackName)
    {
        var client = docker.GetClient();
        var configs = await client.Configs.ListConfigsAsync();

        return configs
            .Where(c => GetStackLabel(c.Spec?.Labels) == stackName)
            .Select(ConfigMapper.ToSwarmConfig)
            .ToList();
    }

    // ──── Private helpers ────

    private static int CountActiveNodes(IEnumerable<NodeListResponse> nodes)
    {
        return nodes.Count(n =>
            n.Status?.State?.ToString()?.ToLower() == "ready"
            && n.Spec?.Availability?.ToLower() == "active");
    }

    private static void ComputeServiceStatus(
        Domain.Docker.SwarmService service,
        IList<TaskResponse> tasks,
        int nodeCount)
    {
        var desiredRunning = tasks
            .Where(t => t.DesiredState.ToString().ToLower() == "running")
            .ToList();

        var runningCount = desiredRunning
            .Count(t => t.Status?.State.ToString().ToLower() == "running");

        var total = service.Mode == "global"
            ? nodeCount
            : service.Replicas ?? 0;

        service.ComputeStatus(runningCount, total);
    }

    private static string? GetStackLabel(IDictionary<string, string>? labels)
    {
        if (labels == null) return null;
        labels.TryGetValue(AppConstants.DockerLabels.StackNamespace, out var stackName);
        return stackName;
    }

    private static string? ParseSinceParameter(string? since)
    {
        if (string.IsNullOrEmpty(since)) return null;

        if (long.TryParse(since, out _))
            return since;

        var now = DateTimeOffset.UtcNow;
        var value = since[..^1];
        var unit = since[^1];

        if (!int.TryParse(value, out var amount))
            return null;

        var offset = unit switch
        {
            's' => TimeSpan.FromSeconds(amount),
            'm' => TimeSpan.FromMinutes(amount),
            'h' => TimeSpan.FromHours(amount),
            'd' => TimeSpan.FromDays(amount),
            _ => TimeSpan.Zero
        };

        if (offset == TimeSpan.Zero)
            return null;

        return (now - offset).ToUnixTimeSeconds().ToString();
    }

    private static async Task<ServiceSpec> BuildServiceSpec(
        DockerClient client,
        CreateServiceParams request)
    {
        var spec = new ServiceSpec
        {
            Name = request.ServiceName,
            TaskTemplate = new TaskSpec
            {
                ContainerSpec = new ContainerSpec
                {
                    Image = request.Image,
                    Args = request.Command,
                    User = request.User,
                    Dir = request.Dir,
                    TTY = request.Tty,
                    Env = request.Variables?
                        .Select(v => $"{v.Name}={v.Value}")
                        .ToList(),
                    Hosts = request.Hosts?
                        .Select(h => $"{h.Name} {h.Value}")
                        .ToList(),
                    Mounts = request.Mounts?
                        .Select(m => new Mount
                        {
                            Type = m.Type,
                            Source = m.Source,
                            Target = m.Target,
                            ReadOnly = m.ReadOnly
                        })
                        .ToList(),
                },
                Networks = request.Networks?
                    .Select(n => new NetworkAttachmentConfig
                    {
                        Target = !string.IsNullOrEmpty(n.Id) ? n.Id : n.NetworkName
                    })
                    .ToList(),
            },
            EndpointSpec = request.Ports is { Count: > 0 }
                ? new EndpointSpec
                {
                    Ports = request.Ports.Select(p => new PortConfig
                    {
                        TargetPort = (uint)p.ContainerPort,
                        PublishedPort = (uint)p.HostPort,
                        Protocol = p.Protocol,
                        PublishMode = p.Mode
                    }).ToList()
                }
                : null,
            Labels = new Dictionary<string, string>()
        };

        // Mode
        if (request.Mode?.ToLower() == "global")
        {
            spec.Mode = new ServiceMode { Global = new GlobalService() };
        }
        else
        {
            spec.Mode = new ServiceMode
            {
                Replicated = new ReplicatedService
                {
                    Replicas = (ulong)request.Replicas
                }
            };
        }

        // Secrets - resolve IDs from names
        if (request.Secrets is { Count: > 0 })
        {
            var allSecrets = await client.Secrets.ListAsync();
            var secretLookup = allSecrets.ToDictionary(s => s.Spec.Name, s => s.ID);

            spec.TaskTemplate.ContainerSpec.Secrets = request.Secrets.Select(s =>
            {
                var secretId = !string.IsNullOrEmpty(s.Id) ? s.Id : "";
                if (string.IsNullOrEmpty(secretId))
                    secretLookup.TryGetValue(s.SecretName, out secretId);

                return new SecretReference
                {
                    SecretID = secretId ?? "",
                    SecretName = s.SecretName,
                    File = new SecretReferenceFileTarget
                    {
                        Name = !string.IsNullOrEmpty(s.SecretTarget) ? s.SecretTarget : s.SecretName,
                        UID = "0",
                        GID = "0",
                        Mode = 292
                    }
                };
            }).ToList();
        }

        // Configs - resolve IDs from names
        if (request.Configs is { Count: > 0 })
        {
            var allConfigs = await client.Configs.ListConfigsAsync();
            var configLookup = allConfigs.ToDictionary(c => c.Spec.Name, c => c.ID);

            spec.TaskTemplate.ContainerSpec.Configs = request.Configs.Select(c =>
            {
                var configId = !string.IsNullOrEmpty(c.Id) ? c.Id : "";
                if (string.IsNullOrEmpty(configId))
                    configLookup.TryGetValue(c.ConfigName, out configId);

                return new SwarmConfigReference
                {
                    ConfigID = configId ?? "",
                    ConfigName = c.ConfigName,
                    File = new ConfigReferenceFileTarget
                    {
                        Name = !string.IsNullOrEmpty(c.ConfigTarget) ? c.ConfigTarget : c.ConfigName,
                        UID = "0",
                        GID = "0",
                        Mode = 292
                    }
                };
            }).ToList();
        }

        // Resources
        if (request.Resources != null)
        {
            spec.TaskTemplate.Resources = new ResourceRequirements();

            if (request.Resources.Reservation != null)
            {
                spec.TaskTemplate.Resources.Reservations = new SwarmResources
                {
                    NanoCPUs = (long)(request.Resources.Reservation.Cpu * 1_000_000_000),
                    MemoryBytes = (long)(request.Resources.Reservation.Memory * 1024 * 1024)
                };
            }

            if (request.Resources.Limit != null)
            {
                spec.TaskTemplate.Resources.Limits = new SwarmLimit
                {
                    NanoCPUs = (long)(request.Resources.Limit.Cpu * 1_000_000_000),
                    MemoryBytes = (long)(request.Resources.Limit.Memory * 1024 * 1024)
                };
            }
        }

        // Deployment
        if (request.Deployment != null)
        {
            if (request.Deployment.RestartPolicy != null)
            {
                var rp = request.Deployment.RestartPolicy;
                spec.TaskTemplate.RestartPolicy = new SwarmRestartPolicy
                {
                    Condition = rp.Condition,
                    Delay = (long)(rp.Delay * 1_000_000_000),
                    MaxAttempts = (ulong)rp.MaxAttempts,
                    Window = (long)(rp.Window * 1_000_000_000)
                };
            }

            if (request.Deployment.Update != null)
            {
                var u = request.Deployment.Update;
                spec.UpdateConfig = new SwarmUpdateConfig
                {
                    Parallelism = (ulong)u.Parallelism,
                    Delay = (long)(u.Delay * 1_000_000_000),
                    FailureAction = u.FailureAction,
                    Monitor = (long)(u.Monitor * 1_000_000_000),
                    Order = u.Order
                };
            }

            if (request.Deployment.Rollback != null)
            {
                var r = request.Deployment.Rollback;
                spec.RollbackConfig = new SwarmUpdateConfig
                {
                    Parallelism = (ulong)r.Parallelism,
                    Delay = (long)(r.Delay * 1_000_000_000),
                    FailureAction = r.FailureAction,
                    Monitor = (long)(r.Monitor * 1_000_000_000),
                    Order = r.Order
                };
            }

            if (request.Deployment.Placement?.Constraints is { Count: > 0 })
            {
                spec.TaskTemplate.Placement = new Placement
                {
                    Constraints = request.Deployment.Placement.Constraints
                };
            }

            if (request.Deployment.AutoRedeploy)
            {
                spec.Labels[AppConstants.DockerLabels.AutoRedeploy] = "true";
            }
        }

        // User labels
        if (request.Labels is { Count: > 0 })
        {
            foreach (var label in request.Labels)
            {
                spec.Labels[label.Name] = label.Value;
            }
        }

        // Log driver
        if (request.Logdriver != null && !string.IsNullOrEmpty(request.Logdriver.Name))
        {
            spec.TaskTemplate.LogDriver = new SwarmDriver
            {
                Name = request.Logdriver.Name,
                Options = request.Logdriver.Opts?
                    .ToDictionary(o => o.Name, o => o.Value)
                    ?? new Dictionary<string, string>()
            };
        }

        return spec;
    }
}
