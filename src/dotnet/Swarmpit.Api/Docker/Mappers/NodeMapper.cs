using Docker.DotNet.Models;
using Swarmpit.Api.Models;

namespace Swarmpit.Api.Docker.Mappers;

public static class NodeMapper
{
    public static SwarmNode ToSwarmNode(NodeListResponse node)
    {
        var role = node.Spec?.Role ?? "";
        var managerAddr = node.ManagerStatus?.Addr;
        var statusAddr = node.Status?.Addr;

        string? address = null;
        if (role == "manager" && managerAddr != null)
        {
            address = managerAddr.Split(':')[0];
        }
        else
        {
            address = statusAddr;
        }

        var resources = node.Description?.Resources;
        var nanoCpu = resources?.NanoCPUs ?? 0;
        var memoryBytes = resources?.MemoryBytes ?? 0;

        var plugins = node.Description?.Engine?.Plugins ?? [];

        return new SwarmNode
        {
            Id = node.ID,
            Version = (long)(node.Version?.Index ?? 0),
            NodeName = node.Description?.Hostname ?? "",
            Role = role,
            Availability = node.Spec?.Availability ?? "",
            Labels = (node.Spec?.Labels ?? new Dictionary<string, string>())
                .Select(kv => new NameValue { Name = kv.Key, Value = kv.Value })
                .ToList(),
            State = node.Status?.State?.ToString() ?? "",
            Address = address,
            Engine = node.Description?.Engine?.EngineVersion,
            Arch = node.Description?.Platform?.Architecture,
            Os = node.Description?.Platform?.OS,
            Resources = new NodeResources
            {
                Cpu = nanoCpu / 1_000_000_000.0,
                Memory = memoryBytes / (1024.0 * 1024.0)
            },
            Plugins = new NodePlugins
            {
                Networks = plugins
                    .Where(p => p.Type == "Network")
                    .Select(p => p.Name)
                    .ToList(),
                Volumes = plugins
                    .Where(p => p.Type == "Volume")
                    .Select(p => p.Name)
                    .ToList()
            },
            Leader = node.ManagerStatus?.Leader
        };
    }
}
