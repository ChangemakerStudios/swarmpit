namespace Swarmpit.Api.Docker;

public class DockerOptions
{
    public const string SectionName = "Docker";

    /// <summary>
    /// Docker socket/endpoint URI. Leave empty for platform auto-detection.
    /// Examples: unix:///var/run/docker.sock, npipe://./pipe/docker_engine, http://localhost:2375
    /// </summary>
    public string? Endpoint { get; set; }
}
