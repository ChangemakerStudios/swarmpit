using Docker.DotNet;

namespace Swarmpit.Api.Docker;

public class DockerClientFactory
{
    private readonly IConfiguration _config;
    private DockerClient? _client;

    public DockerClientFactory(IConfiguration config)
    {
        _config = config;
    }

    public DockerClient GetClient()
    {
        if (_client != null) return _client;

        var dockerSock = _config["SWARMPIT_DOCKER_SOCK"] ?? "/var/run/docker.sock";

        _client = new DockerClientConfiguration(new Uri($"unix://{dockerSock}"))
            .CreateClient();

        return _client;
    }
}
