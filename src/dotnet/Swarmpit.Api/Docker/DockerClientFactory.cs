using Docker.DotNet;
using Microsoft.Extensions.Options;

namespace Swarmpit.Api.Docker;

public class DockerClientFactory(IOptions<DockerOptions> options, ILogger<DockerClientFactory> logger)
{
    private DockerClient? _client;

    public DockerClient GetClient()
    {
        if (_client != null) return _client;

        var endpoint = options.Value.Endpoint;

        if (!string.IsNullOrEmpty(endpoint))
        {
            var uri = new Uri(endpoint.Contains("://") ? endpoint : $"unix://{endpoint}");
            logger.LogInformation("Connecting to Docker at {Endpoint}", uri);
            _client = new DockerClientConfiguration(uri).CreateClient();
        }
        else
        {
            logger.LogInformation("Connecting to Docker using platform default");
            _client = new DockerClientConfiguration().CreateClient();
        }

        return _client;
    }
}
