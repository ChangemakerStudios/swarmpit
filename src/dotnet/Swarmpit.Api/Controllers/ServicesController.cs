using Docker.DotNet.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swarmpit.Api.Docker;
using Swarmpit.Api.Docker.Mappers;
using Swarmpit.Api.Models.Requests;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Swarmpit.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/services")]
public class ServicesController : ControllerBase
{
    private readonly DockerClientFactory _docker;

    public ServicesController(DockerClientFactory docker)
    {
        _docker = docker;
    }

    [HttpGet]
    public async Task<IActionResult> List()
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

        var mapped = services.Select(svc =>
        {
            var service = ServiceMapper.ToSwarmService(svc, networks);
            var serviceTasks = tasksByService.GetValueOrDefault(svc.ID ?? "", []);
            ComputeStatus(service, serviceTasks, nodeCount);
            return service;
        }).ToList();

        return Ok(mapped);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var client = _docker.GetClient();

        global::Docker.DotNet.Models.SwarmService svc;
        try
        {
            svc = await client.Swarm.InspectServiceAsync(id);
        }
        catch (global::Docker.DotNet.DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound();
        }

        var networks = await client.Networks.ListNetworksAsync();
        var tasks = await client.Tasks.ListAsync();
        var nodes = await client.Swarm.ListNodesAsync();

        var serviceTasks = tasks.Where(t => t.ServiceID == id).ToList();

        var nodeCount = nodes.Count(n =>
            n.Status?.State?.ToString()?.ToLower() == "ready"
            && n.Spec?.Availability?.ToLower() == "active");

        var service = ServiceMapper.ToSwarmService(svc, networks);
        ComputeStatus(service, serviceTasks, nodeCount);

        return Ok(service);
    }

    [HttpGet("{id}/tasks")]
    public async Task<IActionResult> Tasks(string id)
    {
        var client = _docker.GetClient();

        var tasks = await client.Tasks.ListAsync();
        var services = await client.Swarm.ListServicesAsync();
        var nodes = await client.Swarm.ListNodesAsync();

        var serviceNames = services.ToDictionary(
            s => s.ID ?? "",
            s => s.Spec?.Name ?? "");

        var nodeNames = nodes.ToDictionary(
            n => n.ID ?? "",
            n => n.Description?.Hostname ?? "");

        var mapped = tasks
            .Where(t => t.ServiceID == id)
            .Select(t => TaskMapper.ToSwarmTask(t, serviceNames, nodeNames))
            .ToList();

        return Ok(mapped);
    }

    [HttpGet("{id}/networks")]
    public async Task<IActionResult> Networks(string id)
    {
        var client = _docker.GetClient();

        global::Docker.DotNet.Models.SwarmService svc;
        try
        {
            svc = await client.Swarm.InspectServiceAsync(id);
        }
        catch (global::Docker.DotNet.DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound();
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

        var mapped = new List<Models.SwarmNetwork>();
        foreach (var networkId in networkIds)
        {
            try
            {
                var network = await client.Networks.InspectNetworkAsync(networkId);
                mapped.Add(NetworkMapper.ToSwarmNetwork(network));
            }
            catch (global::Docker.DotNet.DockerApiException)
            {
                // Network may have been removed; skip it
            }
        }

        return Ok(mapped);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var client = _docker.GetClient();
        await client.Swarm.RemoveServiceAsync(id);
        return NoContent();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateServiceRequest request)
    {
        var client = _docker.GetClient();
        var spec = await BuildServiceSpec(client, request);

        var response = await client.Swarm.CreateServiceAsync(new ServiceCreateParameters
        {
            Service = spec
        });

        return Ok(new { id = response.ID });
    }

    [HttpPost("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] CreateServiceRequest request)
    {
        var client = _docker.GetClient();

        global::Docker.DotNet.Models.SwarmService svc;
        try
        {
            svc = await client.Swarm.InspectServiceAsync(id);
        }
        catch (global::Docker.DotNet.DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound();
        }

        var version = svc.Version?.Index ?? 0;
        var spec = await BuildServiceSpec(client, request);

        await client.Swarm.UpdateServiceAsync(id, new ServiceUpdateParameters
        {
            Service = spec,
            Version = (long)version
        });

        return Ok(new { id });
    }

    [HttpPost("{id}/redeploy")]
    public async Task<IActionResult> Redeploy(string id, [FromQuery] string? tag = null)
    {
        var client = _docker.GetClient();

        global::Docker.DotNet.Models.SwarmService svc;
        try
        {
            svc = await client.Swarm.InspectServiceAsync(id);
        }
        catch (global::Docker.DotNet.DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound();
        }

        var version = svc.Version?.Index ?? 0;
        var spec = svc.Spec;

        if (spec?.TaskTemplate?.ContainerSpec != null)
        {
            // Increment ForceUpdate to trigger redeployment
            spec.TaskTemplate.ForceUpdate = spec.TaskTemplate.ForceUpdate + 1;

            // Optionally update the image tag
            if (!string.IsNullOrEmpty(tag))
            {
                var currentImage = spec.TaskTemplate.ContainerSpec.Image ?? "";
                // Strip any existing digest
                var atIndex = currentImage.IndexOf('@');
                if (atIndex >= 0)
                    currentImage = currentImage[..atIndex];

                // Replace the tag
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

        return Ok(new { id });
    }

    [HttpPost("{id}/rollback")]
    public async Task<IActionResult> Rollback(string id)
    {
        var client = _docker.GetClient();

        global::Docker.DotNet.Models.SwarmService svc;
        try
        {
            svc = await client.Swarm.InspectServiceAsync(id);
        }
        catch (global::Docker.DotNet.DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound();
        }

        if (svc.PreviousSpec == null)
        {
            return BadRequest(new { error = "No previous spec available for rollback" });
        }

        var version = svc.Version?.Index ?? 0;

        await client.Swarm.UpdateServiceAsync(id, new ServiceUpdateParameters
        {
            Service = svc.PreviousSpec,
            Version = (long)version
        });

        return Ok(new { id });
    }

    [HttpPost("{id}/stop")]
    public async Task<IActionResult> Stop(string id)
    {
        var client = _docker.GetClient();

        global::Docker.DotNet.Models.SwarmService svc;
        try
        {
            svc = await client.Swarm.InspectServiceAsync(id);
        }
        catch (global::Docker.DotNet.DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound();
        }

        if (svc.Spec?.Mode?.Replicated == null)
        {
            return BadRequest(new { error = "Stop is only supported for replicated services" });
        }

        var version = svc.Version?.Index ?? 0;
        svc.Spec.Mode.Replicated.Replicas = 0;

        await client.Swarm.UpdateServiceAsync(id, new ServiceUpdateParameters
        {
            Service = svc.Spec,
            Version = (long)version
        });

        return Ok(new { id });
    }

    [HttpGet("{id}/logs")]
    public async Task<IActionResult> Logs(string id, [FromQuery] string? since = null)
    {
        var client = _docker.GetClient();

        // Verify the service exists
        try
        {
            await client.Swarm.InspectServiceAsync(id);
        }
        catch (global::Docker.DotNet.DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound();
        }

        // Docker.DotNet may not expose service logs directly, so use the underlying HTTP client
        // Build the Docker API URL for service logs
        var sinceParam = ParseSinceParameter(since);
        var queryParams = $"stdout=true&stderr=true&tail=500&timestamps=true";
        if (!string.IsNullOrEmpty(sinceParam))
            queryParams += $"&since={sinceParam}";

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
                // Try to parse timestamp from the line
                var timestamp = "";
                var content = line;

                // Timestamps format: 2021-01-01T00:00:00.000000000Z <message>
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

            return Ok(logLines);
        }
        catch (Exception)
        {
            // Fallback: return empty logs if service logs API is not available
            return Ok(Array.Empty<object>());
        }
    }

    [HttpGet("{id}/compose")]
    public async Task<IActionResult> Compose(string id)
    {
        var client = _docker.GetClient();

        global::Docker.DotNet.Models.SwarmService svc;
        try
        {
            svc = await client.Swarm.InspectServiceAsync(id);
        }
        catch (global::Docker.DotNet.DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound();
        }

        var spec = svc.Spec;
        var containerSpec = spec?.TaskTemplate?.ContainerSpec;
        var serviceName = spec?.Name ?? id;

        var serviceConfig = new Dictionary<string, object>();

        // Image
        var image = containerSpec?.Image ?? "";
        var atIdx = image.IndexOf('@');
        if (atIdx >= 0) image = image[..atIdx];
        serviceConfig["image"] = image;

        // Ports
        var ports = svc.Endpoint?.Ports;
        if (ports is { Count: > 0 })
        {
            serviceConfig["ports"] = ports.Select(p =>
                $"{p.PublishedPort}:{p.TargetPort}/{p.Protocol ?? "tcp"}").ToList();
        }

        // Networks
        var taskNetworks = spec?.TaskTemplate?.Networks;
        if (taskNetworks is { Count: > 0 })
        {
            serviceConfig["networks"] = taskNetworks
                .Select(n => n.Target ?? "")
                .Where(n => !string.IsNullOrEmpty(n))
                .ToList();
        }

        // Volumes/Mounts
        var mounts = containerSpec?.Mounts;
        if (mounts is { Count: > 0 })
        {
            serviceConfig["volumes"] = mounts.Select(m =>
            {
                var ro = m.ReadOnly ? ":ro" : "";
                return $"{m.Source}:{m.Target}{ro}";
            }).ToList();
        }

        // Environment
        var env = containerSpec?.Env;
        if (env is { Count: > 0 })
        {
            serviceConfig["environment"] = env.ToList();
        }

        // Deploy section
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

        // Resources
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

        // Placement
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

        var yaml = serializer.Serialize(composeDoc);
        return Content(yaml, "text/yaml");
    }

    // --- Private helpers ---

    private static async Task<ServiceSpec> BuildServiceSpec(
        global::Docker.DotNet.DockerClient client,
        CreateServiceRequest request)
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
                        Mode = 292 // 0444 in octal
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
                        Mode = 292 // 0444 in octal
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
            // Restart policy
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

            // Update config
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

            // Rollback config
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

            // Placement
            if (request.Deployment.Placement?.Constraints is { Count: > 0 })
            {
                spec.TaskTemplate.Placement = new Placement
                {
                    Constraints = request.Deployment.Placement.Constraints
                };
            }

            // AutoRedeploy label
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

    private static string? ParseSinceParameter(string? since)
    {
        if (string.IsNullOrEmpty(since)) return null;

        // If it's a unix timestamp, return as-is
        if (long.TryParse(since, out _))
            return since;

        // Parse relative time like "15m", "1h", "2d"
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

    private static void ComputeStatus(
        Models.SwarmService service,
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
}
