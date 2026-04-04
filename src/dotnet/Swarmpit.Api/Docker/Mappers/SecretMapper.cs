using Docker.DotNet.Models;
using Swarmpit.Api.Models;

namespace Swarmpit.Api.Docker.Mappers;

public static class SecretMapper
{
    public static SwarmSecret ToSwarmSecret(Secret secret)
    {
        return new SwarmSecret
        {
            Id = secret.ID ?? "",
            Version = (long)(secret.Version?.Index ?? 0),
            SecretName = secret.Spec?.Name ?? "",
            CreatedAt = secret.CreatedAt.ToString("o"),
            UpdatedAt = secret.UpdatedAt.ToString("o")
        };
    }
}
