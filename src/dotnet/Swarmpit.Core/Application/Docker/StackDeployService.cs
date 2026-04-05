using Swarmpit.Core.Application.Stacks;
using Swarmpit.Core.Domain;
using Swarmpit.Core.Infrastructure.Docker;
using Docker.DotNet.Models;

namespace Swarmpit.Core.Application.Docker;

public class StackDeployService(DockerClientFactory docker, IStackFileRepository stackFiles) : IStackDeployService
{
    public async Task<StackDeployResult> DeployAsync(string stackName, string composeYaml)
    {
        await stackFiles.SaveAsync(stackName, composeYaml);

        var client = docker.GetClient();
        var result = new StackDeployResult { StackName = stackName };

        var deserializer = new YamlDotNet.Serialization.DeserializerBuilder().Build();
        var compose = deserializer.Deserialize<Dictionary<string, object>>(composeYaml);

        if (compose == null || !compose.ContainsKey("services"))
            throw new InvalidOperationException("Invalid compose file: no services defined");

        var services = compose["services"] as Dictionary<object, object>
            ?? throw new InvalidOperationException("Invalid compose file: services must be a mapping");

        // Create volumes (only if they don't exist)
        var existingVolumes = await client.Volumes.ListAsync();
        var existingVolumeNames = (existingVolumes?.Volumes ?? [])
            .Select(v => v.Name).ToHashSet();

        if (compose.TryGetValue("volumes", out var volumesObj) && volumesObj is Dictionary<object, object> volumeDefs)
        {
            foreach (var volName in volumeDefs.Keys)
            {
                var fullVolName = $"{stackName}_{volName}";
                if (existingVolumeNames.Contains(fullVolName))
                {
                    result.Details.Add($"Volume {fullVolName} exists");
                    continue;
                }

                try
                {
                    await client.Volumes.CreateAsync(new VolumesCreateParameters
                    {
                        Name = fullVolName,
                        Driver = "local",
                        Labels = new Dictionary<string, string>
                        {
                            [AppConstants.DockerLabels.StackNamespace] = stackName
                        }
                    });
                    result.Details.Add($"Created volume {fullVolName}");
                }
                catch (Exception ex)
                {
                    result.Details.Add($"Failed volume {fullVolName}: {ex.Message}");
                }
            }
        }

        // Create networks
        if (compose.TryGetValue("networks", out var networksObj) && networksObj is Dictionary<object, object> networkDefs)
        {
            foreach (var netName in networkDefs.Keys)
            {
                var fullNetName = $"{stackName}_{netName}";
                try
                {
                    await client.Networks.CreateNetworkAsync(new NetworksCreateParameters
                    {
                        Name = fullNetName,
                        Driver = "overlay",
                        Labels = new Dictionary<string, string>
                        {
                            [AppConstants.DockerLabels.StackNamespace] = stackName
                        }
                    });
                    result.Details.Add($"Created network {fullNetName}");
                }
                catch
                {
                    result.Details.Add($"Network {fullNetName} already exists");
                }
            }
        }

        // Create each service
        foreach (var (svcName, svcDef) in services)
        {
            var svcKey = svcName.ToString()!;
            var svcConfig = svcDef as Dictionary<object, object> ?? new();
            var fullSvcName = $"{stackName}_{svcKey}";

            var image = svcConfig.TryGetValue("image", out var img) ? img?.ToString() ?? "" : "";
            if (string.IsNullOrEmpty(image)) continue;

            var spec = new ServiceSpec
            {
                Name = fullSvcName,
                Labels = new Dictionary<string, string>
                {
                    [AppConstants.DockerLabels.StackNamespace] = stackName
                },
                TaskTemplate = new TaskSpec
                {
                    ContainerSpec = new ContainerSpec
                    {
                        Image = image
                    }
                },
                Mode = new ServiceMode
                {
                    Replicated = new ReplicatedService { Replicas = 1 }
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
                var ports = new List<PortConfig>();
                foreach (var p in portItems)
                {
                    var portStr = p.ToString()!;
                    var parts = portStr.Split(':');
                    if (parts.Length == 2 && uint.TryParse(parts[0], out var hostPort) && uint.TryParse(parts[1], out var containerPort))
                    {
                        ports.Add(new PortConfig
                        {
                            PublishedPort = hostPort,
                            TargetPort = containerPort,
                            Protocol = "tcp",
                            PublishMode = "ingress"
                        });
                    }
                }
                spec.EndpointSpec = new EndpointSpec { Ports = ports };
            }

            // Parse volumes/mounts
            if (svcConfig.TryGetValue("volumes", out var volObj) && volObj is List<object> volItems)
            {
                var mounts = new List<Mount>();
                foreach (var v in volItems)
                {
                    var volStr = v.ToString()!;
                    var parts = volStr.Split(':');
                    if (parts.Length >= 2)
                    {
                        var source = parts[0];
                        var target = parts[1];
                        var readOnly = parts.Length > 2 && parts[2] == "ro";

                        var isNamedVolume = !source.StartsWith("/") && !source.StartsWith("./") && !source.StartsWith("../");
                        if (isNamedVolume)
                            source = $"{stackName}_{source}";

                        mounts.Add(new Mount
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
                var existing = (await client.Swarm.ListServicesAsync())
                    .FirstOrDefault(s => s.Spec?.Name == fullSvcName);

                if (existing != null)
                {
                    spec.TaskTemplate.ForceUpdate = (existing.Spec?.TaskTemplate?.ForceUpdate ?? 0) + 1;
                    await client.Swarm.UpdateServiceAsync(existing.ID, new ServiceUpdateParameters
                    {
                        Service = spec,
                        Version = (long)(existing.Version?.Index ?? 0)
                    });
                    result.Details.Add($"Updated {fullSvcName}");
                }
                else
                {
                    await client.Swarm.CreateServiceAsync(new ServiceCreateParameters { Service = spec });
                    result.Details.Add($"Created {fullSvcName}");
                }
            }
            catch (Exception ex)
            {
                result.Details.Add($"Failed {fullSvcName}: {ex.Message}");
            }
        }

        return result;
    }
}
