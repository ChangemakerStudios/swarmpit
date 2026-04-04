using Docker.DotNet.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swarmpit.Api.Docker;
using Swarmpit.Api.Docker.Mappers;

namespace Swarmpit.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/networks")]
public class NetworksController : ControllerBase
{
    private readonly DockerClientFactory _docker;

    public NetworksController(DockerClientFactory docker)
    {
        _docker = docker;
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var client = _docker.GetClient();
        var networks = await client.Networks.ListNetworksAsync();
        var mapped = networks.Select(NetworkMapper.ToSwarmNetwork).ToList();
        return Ok(mapped);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var client = _docker.GetClient();
        try
        {
            var network = await client.Networks.InspectNetworkAsync(id);
            return Ok(NetworkMapper.ToSwarmNetwork(network));
        }
        catch (global::Docker.DotNet.DockerNetworkNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateNetworkRequest request)
    {
        var client = _docker.GetClient();

        var parameters = new NetworksCreateParameters
        {
            Name = request.NetworkName,
            Driver = request.Driver ?? "overlay",
            Internal = request.Internal,
            Attachable = request.Attachable,
            Ingress = request.Ingress,
            EnableIPv6 = request.EnableIPv6,
            Options = request.Options ?? new Dictionary<string, string>()
        };

        if (request.Ipam != null)
        {
            parameters.IPAM = new IPAM
            {
                Config = new List<IPAMConfig>
                {
                    new()
                    {
                        Subnet = request.Ipam.Subnet,
                        Gateway = request.Ipam.Gateway
                    }
                }
            };
        }

        var response = await client.Networks.CreateNetworkAsync(parameters);
        return Ok(new { Id = response.ID });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var client = _docker.GetClient();
        await client.Networks.DeleteNetworkAsync(id);
        return NoContent();
    }

    [HttpGet("{id}/services")]
    public async Task<IActionResult> Services(string id)
    {
        var client = _docker.GetClient();
        var services = await client.Swarm.ListServicesAsync();
        var networks = await client.Networks.ListNetworksAsync();

        var filtered = services
            .Where(svc =>
            {
                var taskNetworks = svc.Spec?.TaskTemplate?.Networks;
                if (taskNetworks != null && taskNetworks.Any(n => n.Target == id))
                    return true;

                var specNetworks = svc.Spec?.Networks;
                if (specNetworks != null && specNetworks.Any(n => n.Target == id))
                    return true;

                return false;
            })
            .Select(svc => ServiceMapper.ToSwarmService(svc, networks))
            .ToList();

        return Ok(filtered);
    }
}

public class CreateNetworkRequest
{
    public string NetworkName { get; set; } = "";
    public string? Driver { get; set; }
    public bool Internal { get; set; }
    public bool Attachable { get; set; }
    public bool Ingress { get; set; }
    public bool EnableIPv6 { get; set; }
    public NetworkIpamRequest? Ipam { get; set; }
    public Dictionary<string, string>? Options { get; set; }
}

public class NetworkIpamRequest
{
    public string? Subnet { get; set; }
    public string? Gateway { get; set; }
}
