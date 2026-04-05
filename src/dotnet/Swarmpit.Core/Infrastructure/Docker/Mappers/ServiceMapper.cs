using global::Docker.DotNet.Models;
using Swarmpit.Core.Domain;
using Swarmpit.Core.Domain.Docker;

namespace Swarmpit.Core.Infrastructure.Docker.Mappers;

public static class ServiceMapper
{
    public static Domain.Docker.SwarmService ToSwarmService(
        global::Docker.DotNet.Models.SwarmService svc,
        IList<NetworkResponse>? networks = null)
    {
        var spec = svc.Spec;
        var taskTemplate = spec?.TaskTemplate;
        var containerSpec = taskTemplate?.ContainerSpec;
        var allLabels = spec?.Labels ?? new Dictionary<string, string>();
        var containerLabels = containerSpec?.Labels ?? new Dictionary<string, string>();

        allLabels.TryGetValue(AppConstants.DockerLabels.StackNamespace, out var stack);

        var mode = spec?.Mode?.Replicated != null ? "replicated" : "global";
        var replicas = spec?.Mode?.Replicated?.Replicas;

        var imageRaw = containerSpec?.Image ?? "";
        var repository = ParseRepository(imageRaw);

        var resources = taskTemplate?.Resources;
        var reservation = resources?.Reservations;
        var limit = resources?.Limits;

        var updateConfig = spec?.UpdateConfig;
        var rollbackConfig = spec?.RollbackConfig;
        var restartPolicy = taskTemplate?.RestartPolicy;
        var placement = taskTemplate?.Placement;

        var logDriver = taskTemplate?.LogDriver;

        var healthcheck = containerSpec?.Healthcheck;

        allLabels.TryGetValue(AppConstants.DockerLabels.Immutable, out var immutableStr);
        allLabels.TryGetValue(AppConstants.DockerLabels.Agent, out var agentStr);
        allLabels.TryGetValue(AppConstants.DockerLabels.AutoRedeploy, out var autoredeployStr);

        var updateStatus = svc.UpdateStatus;

        return new Domain.Docker.SwarmService
        {
            Id = svc.ID ?? "",
            Version = (long)(svc.Version?.Index ?? 0),
            CreatedAt = svc.CreatedAt.ToString("o"),
            UpdatedAt = svc.UpdatedAt.ToString("o"),
            ServiceName = spec?.Name ?? "",
            Mode = mode,
            Replicas = replicas.HasValue ? (int)replicas.Value : null,
            Stack = stack,
            Repository = repository,
            Ports = MapPorts(svc.Endpoint?.Ports),
            Networks = MapNetworks(taskTemplate?.Networks, networks),
            Mounts = MapMounts(containerSpec?.Mounts),
            Variables = MapEnvVariables(containerSpec?.Env),
            Labels = MapLabels(allLabels),
            ContainerLabels = containerLabels
                .Select(kv => new NameValue { Name = kv.Key, Value = kv.Value })
                .ToList(),
            Secrets = MapSecrets(containerSpec?.Secrets),
            Configs = MapConfigs(containerSpec?.Configs),
            Hosts = MapHosts(containerSpec?.Hosts),
            Command = containerSpec?.Command?.ToList() ?? [],
            User = containerSpec?.User,
            Dir = containerSpec?.Dir,
            Tty = containerSpec?.TTY ?? false,
            Resources = new ServiceResources
            {
                Reservation = new ServiceResourceConfig
                {
                    Cpu = (reservation?.NanoCPUs ?? 0) / 1_000_000_000.0,
                    Memory = (reservation?.MemoryBytes ?? 0) / (1024.0 * 1024.0)
                },
                Limit = new ServiceResourceConfig
                {
                    Cpu = (limit?.NanoCPUs ?? 0) / 1_000_000_000.0,
                    Memory = (limit?.MemoryBytes ?? 0) / (1024.0 * 1024.0)
                }
            },
            Deployment = new ServiceDeployment
            {
                Update = MapDeploymentConfig(updateConfig),
                Rollback = MapDeploymentConfig(rollbackConfig),
                RestartPolicy = new ServiceRestartPolicy
                {
                    Condition = restartPolicy?.Condition ?? "",
                    Delay = NanosecondsToSeconds(restartPolicy?.Delay),
                    MaxAttempts = (long)(restartPolicy?.MaxAttempts ?? 0),
                    Window = NanosecondsToSeconds(restartPolicy?.Window)
                },
                ForceUpdate = (long)(taskTemplate?.ForceUpdate ?? 0),
                Placement = new ServicePlacement
                {
                    Constraints = placement?.Constraints?.ToList() ?? []
                },
                AutoRedeploy = autoredeployStr == "true",
                RollbackAllowed = rollbackConfig != null
            },
            Logdriver = new ServiceLogdriver
            {
                Name = logDriver?.Name ?? "",
                Opts = (logDriver?.Options ?? new Dictionary<string, string>())
                    .Select(kv => new NameValue { Name = kv.Key, Value = kv.Value })
                    .ToList()
            },
            Healthcheck = MapHealthcheck(healthcheck),
            Links = MapLinks(allLabels),
            Immutable = immutableStr == "true",
            Agent = agentStr == "true",
            Status = new Domain.Docker.ServiceStatus
            {
                Update = updateStatus?.State,
                Message = updateStatus?.Message
            }
        };
    }

    private static ServiceRepository ParseRepository(string imageRaw)
    {
        var image = imageRaw;
        string? digest = null;

        var atIndex = imageRaw.IndexOf('@');
        if (atIndex >= 0)
        {
            image = imageRaw[..atIndex];
            digest = imageRaw[(atIndex + 1)..];
        }

        var name = image;
        var tag = "latest";

        var colonIndex = image.LastIndexOf(':');
        if (colonIndex >= 0)
        {
            name = image[..colonIndex];
            tag = image[(colonIndex + 1)..];
        }

        return new ServiceRepository
        {
            Name = name,
            Tag = tag,
            Image = image,
            ImageDigest = digest
        };
    }

    private static List<ServicePort> MapPorts(IList<PortConfig>? ports)
    {
        if (ports == null) return [];

        return ports.Select(p => new ServicePort
        {
            ContainerPort = p.TargetPort,
            Protocol = p.Protocol ?? "",
            Mode = p.PublishMode ?? "",
            HostPort = p.PublishedPort
        }).ToList();
    }

    private static List<ServiceNetwork> MapNetworks(
        IList<NetworkAttachmentConfig>? networkAttachments,
        IList<NetworkResponse>? allNetworks)
    {
        if (networkAttachments == null) return [];

        var networkLookup = allNetworks?
            .Where(n => n.ID != null)
            .ToDictionary(n => n.ID!, n => n.Name ?? "")
            ?? new Dictionary<string, string>();

        return networkAttachments.Select(na =>
        {
            var id = na.Target ?? "";
            networkLookup.TryGetValue(id, out var networkName);

            return new ServiceNetwork
            {
                Id = id,
                NetworkName = networkName ?? "",
                ServiceAliases = na.Aliases?.ToList() ?? []
            };
        }).ToList();
    }

    private static List<ServiceMount> MapMounts(IList<Mount>? mounts)
    {
        if (mounts == null) return [];

        return mounts.Select(m => new ServiceMount
        {
            Type = m.Type ?? "",
            Source = m.Source ?? "",
            Target = m.Target ?? "",
            ReadOnly = m.ReadOnly
        }).ToList();
    }

    private static List<NameValue> MapEnvVariables(IList<string>? env)
    {
        if (env == null) return [];

        return env.Select(e =>
        {
            var eqIndex = e.IndexOf('=');
            if (eqIndex >= 0)
            {
                return new NameValue
                {
                    Name = e[..eqIndex],
                    Value = e[(eqIndex + 1)..]
                };
            }

            return new NameValue { Name = e, Value = "" };
        }).ToList();
    }

    private static List<NameValue> MapLabels(IDictionary<string, string> labels)
    {
        return labels
            .Where(kv => !kv.Key.StartsWith(AppConstants.DockerLabels.DockerLabelPrefix) && !kv.Key.StartsWith(AppConstants.DockerLabels.SwarmpitLabelPrefix))
            .Select(kv => new NameValue { Name = kv.Key, Value = kv.Value })
            .ToList();
    }

    private static List<ServiceSecretRef> MapSecrets(IList<SecretReference>? secrets)
    {
        if (secrets == null) return [];

        return secrets.Select(s => new ServiceSecretRef
        {
            Id = s.SecretID ?? "",
            SecretName = s.SecretName ?? "",
            SecretTarget = s.File?.Name ?? ""
        }).ToList();
    }

    private static List<ServiceConfigRef> MapConfigs(IList<SwarmConfigReference>? configs)
    {
        if (configs == null) return [];

        return configs.Select(c => new ServiceConfigRef
        {
            Id = c.ConfigID ?? "",
            ConfigName = c.ConfigName ?? "",
            ConfigTarget = c.File?.Name ?? ""
        }).ToList();
    }

    private static List<NameValue> MapHosts(IList<string>? hosts)
    {
        if (hosts == null) return [];

        return hosts.Select(h =>
        {
            var parts = h.Split(' ', 2);
            if (parts.Length == 2)
            {
                return new NameValue { Name = parts[0], Value = parts[1] };
            }

            return new NameValue { Name = h, Value = "" };
        }).ToList();
    }

    private static List<NameValue> MapLinks(IDictionary<string, string> labels)
    {
        var prefix = AppConstants.DockerLabels.LinkPrefix;

        return labels
            .Where(kv => kv.Key.StartsWith(prefix))
            .Select(kv => new NameValue
            {
                Name = kv.Key[prefix.Length..],
                Value = kv.Value
            })
            .ToList();
    }

    private static ServiceDeploymentConfig MapDeploymentConfig(SwarmUpdateConfig? config)
    {
        if (config == null) return new ServiceDeploymentConfig();

        return new ServiceDeploymentConfig
        {
            Parallelism = (long)config.Parallelism,
            Delay = NanosecondsToSeconds(config.Delay),
            FailureAction = config.FailureAction ?? "",
            Monitor = NanosecondsToSeconds(config.Monitor),
            Order = config.Order ?? ""
        };
    }

    private static ServiceHealthcheck MapHealthcheck(HealthConfig? healthcheck)
    {
        if (healthcheck == null) return new ServiceHealthcheck();

        return new ServiceHealthcheck
        {
            Test = healthcheck.Test?.ToList() ?? new List<string>(),
            Interval = healthcheck.Interval.Ticks * 100,
            Timeout = healthcheck.Timeout.Ticks * 100,
            Retries = healthcheck.Retries
        };
    }

    private static double NanosecondsToSeconds(long? nanoseconds)
    {
        return (nanoseconds ?? 0) / 1_000_000_000.0;
    }
}
