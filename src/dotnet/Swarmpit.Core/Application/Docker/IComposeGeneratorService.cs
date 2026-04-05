namespace Swarmpit.Core.Application.Docker;

/// <summary>
/// Generates docker-compose YAML from live Docker state.
/// </summary>
public interface IComposeGeneratorService
{
    /// <summary>
    /// Generate a full docker-compose.yml for all services in a stack.
    /// </summary>
    Task<string> GenerateStackComposeAsync(string stackName);

    /// <summary>
    /// Generate a docker-compose.yml fragment for a single service.
    /// </summary>
    Task<string> GenerateServiceComposeAsync(string serviceId);
}
