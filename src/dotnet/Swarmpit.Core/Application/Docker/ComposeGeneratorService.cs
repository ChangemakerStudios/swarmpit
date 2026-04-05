using Swarmpit.Core.Domain;
using Swarmpit.Core.Domain.Docker;
using Swarmpit.Core.Infrastructure.Docker;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Swarmpit.Core.Application.Docker;

public class ComposeGeneratorService(DockerClientFactory docker) : IComposeGeneratorService
{
    private static readonly ISerializer YamlSerializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitEmptyCollections)
        .Build();

    public async Task<string> GenerateStackComposeAsync(string stackName)
    {
        var client = docker.GetClient();

        var allServices = await client.Swarm.ListServicesAsync();
        var allNetworks = await client.Networks.ListNetworksAsync();
        var allVolumes = await client.Volumes.ListAsync();

        var stackServices = allServices
            .Where(s => GetStackLabel(s.Spec?.Labels) == stackName)
            .ToList();

        var stackNetworks = allNetworks
            .Where(n => GetStackLabel(n.Labels) == stackName)
            .ToList();

        var stackVolumes = (allVolumes?.Volumes ?? [])
            .Where(v => GetStackLabel(v.Labels) == stackName)
            .ToList();

        var compose = new Dictionary<string, object>();

        // Services
        var services = new Dictionary<string, object>();
        foreach (var svc in stackServices)
        {
            var shortName = StripStackPrefix(svc.Spec?.Name ?? "", stackName);
            services[shortName] = BuildServiceDefinition(svc, stackName);
        }
        compose["services"] = services;

        // Networks (only non-default ones)
        var networkDefs = new Dictionary<string, object>();
        foreach (var net in stackNetworks)
        {
            var shortName = StripStackPrefix(net.Name ?? "", stackName);
            if (shortName is "default" or "") continue;

            var netDef = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(net.Driver) && net.Driver != "overlay")
                netDef["driver"] = net.Driver;
            if (net.Attachable) netDef["attachable"] = true;
            if (net.Internal) netDef["internal"] = true;

            networkDefs[shortName] = netDef.Count > 0 ? netDef : new Dictionary<string, object>();
        }
        if (networkDefs.Count > 0)
            compose["networks"] = networkDefs;

        // Volumes
        var volumeDefs = new Dictionary<string, object>();
        foreach (var vol in stackVolumes)
        {
            var shortName = StripStackPrefix(vol.Name ?? "", stackName);
            var volDef = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(vol.Driver) && vol.Driver != "local")
                volDef["driver"] = vol.Driver;

            volumeDefs[shortName] = volDef.Count > 0 ? volDef : new Dictionary<string, object>();
        }
        if (volumeDefs.Count > 0)
            compose["volumes"] = volumeDefs;

        return YamlSerializer.Serialize(compose);
    }

    public async Task<string> GenerateServiceComposeAsync(string serviceId)
    {
        var client = docker.GetClient();
        var svc = await client.Swarm.InspectServiceAsync(serviceId);
        var stackName = GetStackLabel(svc.Spec?.Labels);

        var compose = new Dictionary<string, object>
        {
            ["services"] = new Dictionary<string, object>
            {
                [svc.Spec?.Name ?? serviceId] = BuildServiceDefinition(svc, stackName)
            }
        };

        return YamlSerializer.Serialize(compose);
    }

    private static Dictionary<string, object> BuildServiceDefinition(
        global::Docker.DotNet.Models.SwarmService svc,
        string? stackName)
    {
        var spec = svc.Spec;
        var containerSpec = spec?.TaskTemplate?.ContainerSpec;
        var def = new Dictionary<string, object>();

        // Image (strip digest)
        var image = containerSpec?.Image ?? "";
        var atIdx = image.IndexOf('@');
        if (atIdx >= 0) image = image[..atIdx];
        def["image"] = image;

        // Command
        var args = containerSpec?.Args;
        if (args is { Count: > 0 })
            def["command"] = args.Count == 1 ? args[0] : string.Join(" ", args);

        // Environment
        var env = containerSpec?.Env;
        if (env is { Count: > 0 })
            def["environment"] = env.ToList();

        // Ports
        var ports = svc.Endpoint?.Ports;
        if (ports is { Count: > 0 })
        {
            def["ports"] = ports.Select(p =>
            {
                var proto = p.Protocol is "tcp" or null ? "" : $"/{p.Protocol}";
                return p.PublishedPort == p.TargetPort
                    ? $"{p.TargetPort}{proto}"
                    : $"{p.PublishedPort}:{p.TargetPort}{proto}";
            }).ToList();
        }

        // Networks
        var taskNetworks = spec?.TaskTemplate?.Networks;
        if (taskNetworks is { Count: > 0 })
        {
            def["networks"] = taskNetworks
                .Select(n => StripStackPrefix(n.Target ?? "", stackName))
                .Where(n => !string.IsNullOrEmpty(n))
                .ToList();
        }

        // Volumes/Mounts
        var mounts = containerSpec?.Mounts;
        if (mounts is { Count: > 0 })
        {
            def["volumes"] = mounts.Select(m =>
            {
                var source = StripStackPrefix(m.Source ?? "", stackName);
                var ro = m.ReadOnly ? ":ro" : "";
                return $"{source}:{m.Target}{ro}";
            }).ToList();
        }

        // Hosts
        var hosts = containerSpec?.Hosts;
        if (hosts is { Count: > 0 })
            def["extra_hosts"] = hosts.ToList();

        // User
        if (!string.IsNullOrEmpty(containerSpec?.User))
            def["user"] = containerSpec.User;

        // Working dir
        if (!string.IsNullOrEmpty(containerSpec?.Dir))
            def["working_dir"] = containerSpec.Dir;

        // TTY
        if (containerSpec?.TTY == true)
            def["tty"] = true;

        // Deploy
        var deploy = BuildDeploySection(spec);
        if (deploy.Count > 0)
            def["deploy"] = deploy;

        // Logging
        var logDriver = spec?.TaskTemplate?.LogDriver;
        if (logDriver != null && !string.IsNullOrEmpty(logDriver.Name))
        {
            var logging = new Dictionary<string, object> { ["driver"] = logDriver.Name };
            if (logDriver.Options is { Count: > 0 })
                logging["options"] = new Dictionary<string, string>(logDriver.Options);
            def["logging"] = logging;
        }

        // Secrets
        var secrets = containerSpec?.Secrets;
        if (secrets is { Count: > 0 })
            def["secrets"] = secrets.Select(s => s.SecretName ?? "").Where(s => s != "").ToList();

        // Configs
        var configs = containerSpec?.Configs;
        if (configs is { Count: > 0 })
            def["configs"] = configs.Select(c => c.ConfigName ?? "").Where(c => c != "").ToList();

        return def;
    }

    private static Dictionary<string, object> BuildDeploySection(
        global::Docker.DotNet.Models.ServiceSpec? spec)
    {
        var deploy = new Dictionary<string, object>();

        // Mode & replicas
        var replicated = spec?.Mode?.Replicated;
        if (replicated != null)
        {
            var replicas = (int)(replicated.Replicas ?? 1);
            if (replicas != 1) deploy["replicas"] = replicas;
        }
        else if (spec?.Mode?.Global != null)
        {
            deploy["mode"] = "global";
        }

        // Resources
        var resources = spec?.TaskTemplate?.Resources;
        if (resources != null)
        {
            var res = new Dictionary<string, object>();
            if (resources.Limits != null && (resources.Limits.NanoCPUs > 0 || resources.Limits.MemoryBytes > 0))
            {
                var limits = new Dictionary<string, object>();
                if (resources.Limits.NanoCPUs > 0)
                    limits["cpus"] = $"{resources.Limits.NanoCPUs / 1_000_000_000.0:F2}";
                if (resources.Limits.MemoryBytes > 0)
                    limits["memory"] = FormatBytes(resources.Limits.MemoryBytes);
                res["limits"] = limits;
            }
            if (resources.Reservations != null && (resources.Reservations.NanoCPUs > 0 || resources.Reservations.MemoryBytes > 0))
            {
                var reservations = new Dictionary<string, object>();
                if (resources.Reservations.NanoCPUs > 0)
                    reservations["cpus"] = $"{resources.Reservations.NanoCPUs / 1_000_000_000.0:F2}";
                if (resources.Reservations.MemoryBytes > 0)
                    reservations["memory"] = FormatBytes(resources.Reservations.MemoryBytes);
                res["reservations"] = reservations;
            }
            if (res.Count > 0) deploy["resources"] = res;
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

        // Update config
        var update = spec?.UpdateConfig;
        if (update != null)
        {
            var updateDef = new Dictionary<string, object>();
            if (update.Parallelism > 0) updateDef["parallelism"] = (int)update.Parallelism;
            if (update.Delay > 0) updateDef["delay"] = $"{update.Delay / 1_000_000_000}s";
            if (!string.IsNullOrEmpty(update.Order)) updateDef["order"] = update.Order;
            if (!string.IsNullOrEmpty(update.FailureAction)) updateDef["failure_action"] = update.FailureAction;
            if (updateDef.Count > 0) deploy["update_config"] = updateDef;
        }

        // Restart policy
        var restart = spec?.TaskTemplate?.RestartPolicy;
        if (restart != null && !string.IsNullOrEmpty(restart.Condition))
        {
            var restartDef = new Dictionary<string, object> { ["condition"] = restart.Condition };
            if (restart.Delay > 0) restartDef["delay"] = $"{restart.Delay / 1_000_000_000}s";
            if (restart.MaxAttempts > 0) restartDef["max_attempts"] = (int)restart.MaxAttempts;
            if (restart.Window > 0) restartDef["window"] = $"{restart.Window / 1_000_000_000}s";
            deploy["restart_policy"] = restartDef;
        }

        return deploy;
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes >= 1024 * 1024 * 1024) return $"{bytes / (1024 * 1024 * 1024)}G";
        if (bytes >= 1024 * 1024) return $"{bytes / (1024 * 1024)}M";
        if (bytes >= 1024) return $"{bytes / 1024}K";
        return $"{bytes}";
    }

    private static string StripStackPrefix(string name, string? stackName)
    {
        if (stackName == null) return name;
        var prefix = $"{stackName}_";
        return name.StartsWith(prefix) ? name[prefix.Length..] : name;
    }

    private static string? GetStackLabel(IDictionary<string, string>? labels)
    {
        if (labels == null) return null;
        labels.TryGetValue(AppConstants.DockerLabels.StackNamespace, out var stackName);
        return stackName;
    }
}
