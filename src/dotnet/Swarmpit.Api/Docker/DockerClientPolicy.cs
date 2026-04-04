using Docker.DotNet;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

namespace Swarmpit.Api.Docker;

public class DockerClientPolicy(IOptions<DockerOptions> options, ILogger<DockerClientPolicy> logger)
    : IPooledObjectPolicy<DockerClient>
{
    public DockerClient Create()
    {
        var endpoint = options.Value.Endpoint;

        DockerClient client;
        if (!string.IsNullOrEmpty(endpoint))
        {
            var uri = new Uri(endpoint.Contains("://") ? endpoint : $"unix://{endpoint}");
            logger.LogInformation("Creating Docker client for {Endpoint}", uri);
            client = new DockerClientConfiguration(uri).CreateClient();
        }
        else
        {
            logger.LogInformation("Creating Docker client using platform default");
            client = new DockerClientConfiguration().CreateClient();
        }

        return client;
    }

    public bool Return(DockerClient client) => true;
}
