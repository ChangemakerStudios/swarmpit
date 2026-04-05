using Docker.DotNet.Models;
using Swarmpit.Core.Domain;
using Swarmpit.Core.Domain.Docker;

namespace Swarmpit.Core.Infrastructure.Docker.Mappers;

public static class NetworkMapper
{
    public static SwarmNetwork ToSwarmNetwork(NetworkResponse network)
    {
        var labels = network.Labels ?? new Dictionary<string, string>();
        labels.TryGetValue(AppConstants.DockerLabels.StackNamespace, out var stack);

        var ipamConfig = network.IPAM?.Config?.FirstOrDefault();

        return new SwarmNetwork
        {
            Id = network.ID ?? "",
            NetworkName = network.Name ?? "",
            Created = network.Created.ToString("o"),
            Scope = network.Scope ?? "",
            Driver = network.Driver ?? "",
            Internal = network.Internal,
            Options = (network.Options ?? new Dictionary<string, string>())
                .Select(kv => new NameValue { Name = kv.Key, Value = kv.Value })
                .ToList(),
            Attachable = network.Attachable,
            Ingress = network.Ingress,
            EnableIPv6 = network.EnableIPv6,
            Labels = new Dictionary<string, string>(labels),
            Stack = stack,
            Ipam = new NetworkIpam
            {
                Subnet = ipamConfig?.Subnet,
                Gateway = ipamConfig?.Gateway
            }
        };
    }
}
