using Docker.DotNet.Models;
using Swarmpit.Core.Domain;
using Swarmpit.Core.Domain.Docker;

namespace Swarmpit.Core.Infrastructure.Docker.Mappers;

public static class VolumeMapper
{
    public static SwarmVolume ToSwarmVolume(VolumeResponse volume)
    {
        var labels = volume.Labels ?? new Dictionary<string, string>();
        labels.TryGetValue(AppConstants.DockerLabels.StackNamespace, out var stack);

        return new SwarmVolume
        {
            Id = volume.Name ?? "",
            VolumeName = volume.Name ?? "",
            Driver = volume.Driver ?? "",
            Scope = volume.Scope ?? "",
            Labels = new Dictionary<string, string>(labels),
            Stack = stack,
            Options = (volume.Options ?? new Dictionary<string, string>())
                .Select(kv => new NameValue { Name = kv.Key, Value = kv.Value })
                .ToList(),
            Mountpoint = volume.Mountpoint
        };
    }
}
