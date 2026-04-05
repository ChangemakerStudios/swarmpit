using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swarmpit.Api.Data.CouchDb;
using Swarmpit.Api.Docker;
using Swarmpit.Api.Docker.Mappers;
using Swarmpit.Api.Models;
using Swarmpit.Api.Models.Requests;

namespace Swarmpit.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/stacks")]
public class StacksController : ControllerBase
{
    private readonly DockerClientFactory _docker;
    private readonly StackFileRepository _stackFiles;

    public StacksController(DockerClientFactory docker, StackFileRepository stackFiles)
    {
        _docker = docker;
        _stackFiles = stackFiles;
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var client = _docker.GetClient();

        var services = await client.Swarm.ListServicesAsync();
        var networks = await client.Networks.ListNetworksAsync();
        var volumes = await client.Volumes.ListAsync();
        var configs = await client.Configs.ListConfigsAsync();
        var secrets = await client.Secrets.ListAsync();
        var stackFileDocs = await _stackFiles.GetAllAsync();

        var stacks = new Dictionary<string, SwarmStackStats>();

        // Count services per stack
        foreach (var svc in services)
        {
            var labels = svc.Spec?.Labels ?? new Dictionary<string, string>();
            if (labels.TryGetValue(AppConstants.DockerLabels.StackNamespace, out var stackName)
                && !string.IsNullOrEmpty(stackName))
            {
                if (!stacks.ContainsKey(stackName))
                    stacks[stackName] = new SwarmStackStats();
                stacks[stackName].Services++;
            }
        }

        // Count networks per stack
        foreach (var net in networks)
        {
            var labels = net.Labels ?? new Dictionary<string, string>();
            if (labels.TryGetValue(AppConstants.DockerLabels.StackNamespace, out var stackName)
                && !string.IsNullOrEmpty(stackName))
            {
                if (!stacks.ContainsKey(stackName))
                    stacks[stackName] = new SwarmStackStats();
                stacks[stackName].Networks++;
            }
        }

        // Count volumes per stack
        if (volumes?.Volumes != null)
        {
            foreach (var vol in volumes.Volumes)
            {
                var labels = vol.Labels ?? new Dictionary<string, string>();
                if (labels.TryGetValue(AppConstants.DockerLabels.StackNamespace, out var stackName)
                    && !string.IsNullOrEmpty(stackName))
                {
                    if (!stacks.ContainsKey(stackName))
                        stacks[stackName] = new SwarmStackStats();
                    stacks[stackName].Volumes++;
                }
            }
        }

        // Count configs per stack
        foreach (var cfg in configs)
        {
            var labels = cfg.Spec?.Labels ?? new Dictionary<string, string>();
            if (labels.TryGetValue(AppConstants.DockerLabels.StackNamespace, out var stackName)
                && !string.IsNullOrEmpty(stackName))
            {
                if (!stacks.ContainsKey(stackName))
                    stacks[stackName] = new SwarmStackStats();
                stacks[stackName].Configs++;
            }
        }

        // Count secrets per stack
        foreach (var sec in secrets)
        {
            var labels = sec.Spec?.Labels ?? new Dictionary<string, string>();
            if (labels.TryGetValue(AppConstants.DockerLabels.StackNamespace, out var stackName)
                && !string.IsNullOrEmpty(stackName))
            {
                if (!stacks.ContainsKey(stackName))
                    stacks[stackName] = new SwarmStackStats();
                stacks[stackName].Secrets++;
            }
        }

        // Build stack file lookup
        var stackFileNames = stackFileDocs.Select(sf => sf.Name).ToHashSet();

        var result = new List<SwarmStack>();

        // Add deployed stacks
        foreach (var (name, stats) in stacks)
        {
            result.Add(new SwarmStack
            {
                StackName = name,
                State = "deployed",
                StackFile = stackFileNames.Contains(name),
                Stats = stats
            });
        }

        // Add inactive stacks (have stackfile but no deployed resources)
        foreach (var sf in stackFileDocs)
        {
            if (!stacks.ContainsKey(sf.Name))
            {
                result.Add(new SwarmStack
                {
                    StackName = sf.Name,
                    State = "inactive",
                    StackFile = true,
                    Stats = new SwarmStackStats()
                });
            }
        }

        return Ok(result.OrderBy(s => s.StackName));
    }

    [HttpGet("{name}")]
    public async Task<IActionResult> Get(string name)
    {
        var client = _docker.GetClient();

        var allServices = await client.Swarm.ListServicesAsync();
        var allNetworks = await client.Networks.ListNetworksAsync();
        var allVolumes = await client.Volumes.ListAsync();
        var allConfigs = await client.Configs.ListConfigsAsync();
        var allSecrets = await client.Secrets.ListAsync();

        var stackServices = allServices
            .Where(s => GetStackLabel(s.Spec?.Labels) == name)
            .ToList();

        var stackNetworks = allNetworks
            .Where(n => GetStackLabel(n.Labels) == name)
            .ToList();

        var stackVolumes = (allVolumes?.Volumes ?? [])
            .Where(v => GetStackLabel(v.Labels) == name)
            .ToList();

        var stackConfigs = allConfigs
            .Where(c => GetStackLabel(c.Spec?.Labels) == name)
            .ToList();

        var stackSecrets = allSecrets
            .Where(s => GetStackLabel(s.Spec?.Labels) == name)
            .ToList();

        if (stackServices.Count == 0 && stackNetworks.Count == 0 && stackVolumes.Count == 0
            && stackConfigs.Count == 0 && stackSecrets.Count == 0)
        {
            // Check if there's a stackfile for an inactive stack
            var sf = await _stackFiles.GetByNameAsync(name);
            if (sf == null) return NotFound();

            return Ok(new SwarmStackDetail
            {
                StackName = name,
                State = "inactive",
                StackFile = true,
                Stats = new SwarmStackStats()
            });
        }

        var stackFile = await _stackFiles.GetByNameAsync(name);

        var allTasks = await client.Tasks.ListAsync();
        var allNodes = await client.Swarm.ListNodesAsync();
        var tasksByService = allTasks
            .GroupBy(t => t.ServiceID ?? "")
            .ToDictionary(g => g.Key, g => g.ToList());
        var nodeCount = allNodes.Count(n =>
            n.Status?.State?.ToString()?.ToLower() == "ready"
            && n.Spec?.Availability?.ToLower() == "active");

        var mappedServices = stackServices
            .Select(s =>
            {
                var svc = ServiceMapper.ToSwarmService(s, allNetworks);
                var svcTasks = tasksByService.GetValueOrDefault(s.ID ?? "", []);
                ComputeStatus(svc, svcTasks, nodeCount);
                return svc;
            })
            .ToList();

        var mappedNetworks = stackNetworks
            .Select(NetworkMapper.ToSwarmNetwork)
            .ToList();

        var mappedVolumes = stackVolumes
            .Select(VolumeMapper.ToSwarmVolume)
            .ToList();

        var mappedConfigs = stackConfigs
            .Select(ConfigMapper.ToSwarmConfig)
            .ToList();

        var mappedSecrets = stackSecrets
            .Select(SecretMapper.ToSwarmSecret)
            .ToList();

        return Ok(new SwarmStackDetail
        {
            StackName = name,
            State = "deployed",
            StackFile = stackFile != null,
            Stats = new SwarmStackStats
            {
                Services = mappedServices.Count,
                Networks = mappedNetworks.Count,
                Volumes = mappedVolumes.Count,
                Configs = mappedConfigs.Count,
                Secrets = mappedSecrets.Count
            },
            Services = mappedServices,
            Networks = mappedNetworks,
            Volumes = mappedVolumes,
            Configs = mappedConfigs,
            Secrets = mappedSecrets
        });
    }

    [HttpGet("{name}/services")]
    public async Task<IActionResult> Services(string name)
    {
        var client = _docker.GetClient();
        var services = await client.Swarm.ListServicesAsync();
        var networks = await client.Networks.ListNetworksAsync();
        var tasks = await client.Tasks.ListAsync();
        var nodes = await client.Swarm.ListNodesAsync();

        var tasksByService = tasks
            .GroupBy(t => t.ServiceID ?? "")
            .ToDictionary(g => g.Key, g => g.ToList());

        var nodeCount = nodes.Count(n =>
            n.Status?.State?.ToString()?.ToLower() == "ready"
            && n.Spec?.Availability?.ToLower() == "active");

        var mapped = services
            .Where(s => GetStackLabel(s.Spec?.Labels) == name)
            .Select(s =>
            {
                var svc = ServiceMapper.ToSwarmService(s, networks);
                var svcTasks = tasksByService.GetValueOrDefault(s.ID ?? "", []);
                ComputeStatus(svc, svcTasks, nodeCount);
                return svc;
            })
            .ToList();

        return Ok(mapped);
    }

    private static void ComputeStatus(
        Models.SwarmService service,
        IList<global::Docker.DotNet.Models.TaskResponse> tasks,
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

        service.Status.Tasks = new Models.ServiceTaskStatus
        {
            Running = runningCount,
            Total = total
        };

        if (runningCount == total && total > 0)
            service.State = "running";
        else if (runningCount == 0)
            service.State = "not running";
        else
            service.State = "partly running";
    }

    [HttpGet("{name}/networks")]
    public async Task<IActionResult> Networks(string name)
    {
        var client = _docker.GetClient();
        var networks = await client.Networks.ListNetworksAsync();

        var mapped = networks
            .Where(n => GetStackLabel(n.Labels) == name)
            .Select(NetworkMapper.ToSwarmNetwork)
            .ToList();

        return Ok(mapped);
    }

    [HttpGet("{name}/volumes")]
    public async Task<IActionResult> Volumes(string name)
    {
        var client = _docker.GetClient();
        var volumes = await client.Volumes.ListAsync();

        var mapped = (volumes?.Volumes ?? [])
            .Where(v => GetStackLabel(v.Labels) == name)
            .Select(VolumeMapper.ToSwarmVolume)
            .ToList();

        return Ok(mapped);
    }

    [HttpGet("{name}/configs")]
    public async Task<IActionResult> Configs(string name)
    {
        var client = _docker.GetClient();
        var configs = await client.Configs.ListConfigsAsync();

        var mapped = configs
            .Where(c => GetStackLabel(c.Spec?.Labels) == name)
            .Select(ConfigMapper.ToSwarmConfig)
            .ToList();

        return Ok(mapped);
    }

    [HttpGet("{name}/secrets")]
    public async Task<IActionResult> Secrets(string name)
    {
        var client = _docker.GetClient();
        var secrets = await client.Secrets.ListAsync();

        var mapped = secrets
            .Where(s => GetStackLabel(s.Spec?.Labels) == name)
            .Select(SecretMapper.ToSwarmSecret)
            .ToList();

        return Ok(mapped);
    }

    [HttpGet("{name}/file")]
    public async Task<IActionResult> GetFile(string name)
    {
        var stackFile = await _stackFiles.GetByNameAsync(name);
        if (stackFile == null) return NotFound();

        return Ok(new
        {
            spec = stackFile.Spec != null ? new { compose = stackFile.Spec.Compose } : null,
            previousSpec = stackFile.PreviousSpec != null ? new { compose = stackFile.PreviousSpec.Compose } : null
        });
    }

    [HttpPost("{name}/file")]
    public async Task<IActionResult> SaveFile(string name, [FromBody] SaveStackFileRequest request)
    {
        await _stackFiles.SaveAsync(name, request.Compose);
        return Ok(new { message = "Stackfile saved" });
    }

    [HttpDelete("{name}")]
    public async Task<IActionResult> Delete(string name)
    {
        var client = _docker.GetClient();

        // Remove all services belonging to this stack
        var services = await client.Swarm.ListServicesAsync();
        var stackServices = services
            .Where(s => GetStackLabel(s.Spec?.Labels) == name)
            .ToList();

        foreach (var svc in stackServices)
        {
            await client.Swarm.RemoveServiceAsync(svc.ID);
        }

        // Delete stackfile from CouchDB if exists
        await _stackFiles.DeleteAsync(name);

        return Ok(new { message = $"Stack '{name}' removed ({stackServices.Count} services deleted)" });
    }

    [HttpPost("{name}/deploy")]
    public async Task<IActionResult> Deploy(string name, [FromBody] SaveStackFileRequest request)
    {
        // Save the stackfile
        await _stackFiles.SaveAsync(name, request.Compose);

        // Parse compose YAML and create services via Docker API
        var client = _docker.GetClient();
        var created = new List<string>();

        try
        {
            var deserializer = new YamlDotNet.Serialization.DeserializerBuilder().Build();
            var compose = deserializer.Deserialize<Dictionary<string, object>>(request.Compose);

            if (compose == null || !compose.ContainsKey("services"))
                return BadRequest(new { error = "Invalid compose file: no services defined" });

            var services = compose["services"] as Dictionary<object, object>;
            if (services == null)
                return BadRequest(new { error = "Invalid compose file: services must be a mapping" });

            // Create volumes (only if they don't exist)
            var existingVolumes = await client.Volumes.ListAsync();
            var existingVolumeNames = (existingVolumes?.Volumes ?? [])
                .Select(v => v.Name).ToHashSet();

            if (compose.TryGetValue("volumes", out var volumesObj) && volumesObj is Dictionary<object, object> volumeDefs)
            {
                foreach (var volName in volumeDefs.Keys)
                {
                    var fullVolName = $"{name}_{volName}";
                    if (existingVolumeNames.Contains(fullVolName))
                    {
                        created.Add($"Volume {fullVolName} exists");
                        continue;
                    }

                    try
                    {
                        await client.Volumes.CreateAsync(new global::Docker.DotNet.Models.VolumesCreateParameters
                        {
                            Name = fullVolName,
                            Driver = "local",
                            Labels = new Dictionary<string, string>
                            {
                                [AppConstants.DockerLabels.StackNamespace] = name
                            }
                        });
                        created.Add($"Created volume {fullVolName}");
                    }
                    catch (Exception ex)
                    {
                        created.Add($"Failed volume {fullVolName}: {ex.Message}");
                    }
                }
            }

            // Create networks
            if (compose.TryGetValue("networks", out var networksObj) && networksObj is Dictionary<object, object> networkDefs)
            {
                foreach (var netName in networkDefs.Keys)
                {
                    var fullNetName = $"{name}_{netName}";
                    try
                    {
                        await client.Networks.CreateNetworkAsync(new global::Docker.DotNet.Models.NetworksCreateParameters
                        {
                            Name = fullNetName,
                            Driver = "overlay",
                            Labels = new Dictionary<string, string>
                            {
                                [AppConstants.DockerLabels.StackNamespace] = name
                            }
                        });
                        created.Add($"Created network {fullNetName}");
                    }
                    catch
                    {
                        created.Add($"Network {fullNetName} already exists");
                    }
                }
            }

            // Create each service
            foreach (var (svcName, svcDef) in services)
            {
                var svcKey = svcName.ToString()!;
                var svcConfig = svcDef as Dictionary<object, object> ?? new();
                var fullSvcName = $"{name}_{svcKey}";

                var image = svcConfig.TryGetValue("image", out var img) ? img?.ToString() ?? "" : "";
                if (string.IsNullOrEmpty(image)) continue;

                var spec = new global::Docker.DotNet.Models.ServiceSpec
                {
                    Name = fullSvcName,
                    Labels = new Dictionary<string, string>
                    {
                        [AppConstants.DockerLabels.StackNamespace] = name
                    },
                    TaskTemplate = new global::Docker.DotNet.Models.TaskSpec
                    {
                        ContainerSpec = new global::Docker.DotNet.Models.ContainerSpec
                        {
                            Image = image
                        }
                    },
                    Mode = new global::Docker.DotNet.Models.ServiceMode
                    {
                        Replicated = new global::Docker.DotNet.Models.ReplicatedService { Replicas = 1 }
                    }
                };

                // Parse environment
                if (svcConfig.TryGetValue("environment", out var envObj))
                {
                    var envList = new List<string>();
                    if (envObj is List<object> envItems)
                    {
                        foreach (var e in envItems)
                            envList.Add(e.ToString()!);
                    }
                    spec.TaskTemplate.ContainerSpec.Env = envList;
                }

                // Parse ports
                if (svcConfig.TryGetValue("ports", out var portsObj) && portsObj is List<object> portItems)
                {
                    var ports = new List<global::Docker.DotNet.Models.PortConfig>();
                    foreach (var p in portItems)
                    {
                        var portStr = p.ToString()!;
                        var parts = portStr.Split(':');
                        if (parts.Length == 2 && uint.TryParse(parts[0], out var hostPort) && uint.TryParse(parts[1], out var containerPort))
                        {
                            ports.Add(new global::Docker.DotNet.Models.PortConfig
                            {
                                PublishedPort = hostPort,
                                TargetPort = containerPort,
                                Protocol = "tcp",
                                PublishMode = "ingress"
                            });
                        }
                    }
                    spec.EndpointSpec = new global::Docker.DotNet.Models.EndpointSpec { Ports = ports };
                }

                // Parse volumes/mounts
                if (svcConfig.TryGetValue("volumes", out var volObj) && volObj is List<object> volItems)
                {
                    var mounts = new List<global::Docker.DotNet.Models.Mount>();
                    foreach (var v in volItems)
                    {
                        var volStr = v.ToString()!;
                        var parts = volStr.Split(':');
                        if (parts.Length >= 2)
                        {
                            var source = parts[0];
                            var target = parts[1];
                            var readOnly = parts.Length > 2 && parts[2] == "ro";

                            // If source doesn't start with / or ./ it's a named volume — prefix with stack name
                            var isNamedVolume = !source.StartsWith("/") && !source.StartsWith("./") && !source.StartsWith("../");
                            if (isNamedVolume)
                                source = $"{name}_{source}";

                            mounts.Add(new global::Docker.DotNet.Models.Mount
                            {
                                Type = isNamedVolume ? "volume" : "bind",
                                Source = source,
                                Target = target,
                                ReadOnly = readOnly
                            });
                        }
                    }
                    spec.TaskTemplate.ContainerSpec.Mounts = mounts;
                }

                // Parse replicas from deploy
                if (svcConfig.TryGetValue("deploy", out var deployObj) && deployObj is Dictionary<object, object> deploy)
                {
                    if (deploy.TryGetValue("replicas", out var replicas) && int.TryParse(replicas?.ToString(), out var r))
                    {
                        spec.Mode.Replicated!.Replicas = (ulong)r;
                    }
                }

                try
                {
                    // Check if service already exists, update if so
                    var existing = (await client.Swarm.ListServicesAsync())
                        .FirstOrDefault(s => s.Spec?.Name == fullSvcName);

                    if (existing != null)
                    {
                        spec.TaskTemplate.ForceUpdate = (existing.Spec?.TaskTemplate?.ForceUpdate ?? 0) + 1;
                        await client.Swarm.UpdateServiceAsync(existing.ID, new global::Docker.DotNet.Models.ServiceUpdateParameters
                        {
                            Service = spec,
                            Version = (long)(existing.Version?.Index ?? 0)
                        });
                        created.Add($"Updated {fullSvcName}");
                    }
                    else
                    {
                        await client.Swarm.CreateServiceAsync(new global::Docker.DotNet.Models.ServiceCreateParameters { Service = spec });
                        created.Add($"Created {fullSvcName}");
                    }
                }
                catch (Exception ex)
                {
                    created.Add($"Failed {fullSvcName}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = $"Failed to parse compose: {ex.Message}" });
        }

        return Ok(new { message = $"Stack '{name}' deployed", details = created });
    }

    [HttpPost("{name}/redeploy")]
    public async Task<IActionResult> Redeploy(string name)
    {
        var stackFile = await _stackFiles.GetByNameAsync(name);
        if (stackFile?.Spec == null)
            return BadRequest(new { error = "No stackfile found to redeploy" });

        return await Deploy(name, new SaveStackFileRequest { Compose = stackFile.Spec.Compose });
    }

    private static string? GetStackLabel(IDictionary<string, string>? labels)
    {
        if (labels == null) return null;
        if (labels.TryGetValue(AppConstants.DockerLabels.StackNamespace, out var stackName))
            return stackName;
        if (labels.TryGetValue(AppConstants.DockerLabels.ComposeProject, out var composeName))
            return composeName;
        return null;
    }
}
