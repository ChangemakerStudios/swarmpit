using Swarmpit.Core.Domain.Docker;

namespace Swarmpit.Core.Infrastructure.Docker.Mappers;

public static class ConfigMapper
{
    public static SwarmConfig ToSwarmConfig(global::Docker.DotNet.Models.SwarmConfig config)
    {
        var data = config.Spec?.Data != null
            ? Convert.ToBase64String(config.Spec.Data.ToArray())
            : null;

        return new SwarmConfig
        {
            Id = config.ID ?? "",
            Version = (long)(config.Version?.Index ?? 0),
            ConfigName = config.Spec?.Name ?? "",
            CreatedAt = config.CreatedAt.ToString("o"),
            UpdatedAt = config.UpdatedAt.ToString("o"),
            Data = data
        };
    }
}
