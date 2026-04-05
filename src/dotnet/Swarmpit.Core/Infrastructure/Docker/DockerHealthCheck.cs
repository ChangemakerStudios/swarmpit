using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Swarmpit.Core.Infrastructure.Docker;

public class DockerHealthCheck(DockerClientFactory docker) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = docker.GetClient();
            await client.System.PingAsync(cancellationToken);
            return HealthCheckResult.Healthy("Docker daemon is reachable");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Docker daemon is not reachable", ex);
        }
    }
}
