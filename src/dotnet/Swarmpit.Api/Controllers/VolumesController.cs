using Docker.DotNet.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swarmpit.Api.Docker;
using Swarmpit.Api.Docker.Mappers;

namespace Swarmpit.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/volumes")]
public class VolumesController : ControllerBase
{
    private readonly DockerClientFactory _docker;

    public VolumesController(DockerClientFactory docker)
    {
        _docker = docker;
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var client = _docker.GetClient();
        var response = await client.Volumes.ListAsync();
        var mapped = (response.Volumes ?? [])
            .Select(VolumeMapper.ToSwarmVolume)
            .ToList();
        return Ok(mapped);
    }

    [HttpGet("{name}")]
    public async Task<IActionResult> Get(string name)
    {
        var client = _docker.GetClient();
        try
        {
            var volume = await client.Volumes.InspectAsync(name);
            return Ok(VolumeMapper.ToSwarmVolume(volume));
        }
        catch (global::Docker.DotNet.DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound();
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateVolumeRequest request)
    {
        var client = _docker.GetClient();

        var parameters = new VolumesCreateParameters
        {
            Name = request.VolumeName,
            Driver = request.Driver ?? "local",
            DriverOpts = request.Options ?? new Dictionary<string, string>(),
            Labels = request.Labels ?? new Dictionary<string, string>()
        };

        var volume = await client.Volumes.CreateAsync(parameters);
        return Ok(VolumeMapper.ToSwarmVolume(volume));
    }

    [HttpDelete("{name}")]
    public async Task<IActionResult> Delete(string name)
    {
        var client = _docker.GetClient();
        await client.Volumes.RemoveAsync(name);
        return NoContent();
    }

    [HttpGet("{name}/services")]
    public async Task<IActionResult> Services(string name)
    {
        var client = _docker.GetClient();
        var services = await client.Swarm.ListServicesAsync();
        var networks = await client.Networks.ListNetworksAsync();

        var filtered = services
            .Where(svc =>
            {
                var mounts = svc.Spec?.TaskTemplate?.ContainerSpec?.Mounts;
                return mounts != null && mounts.Any(m => m.Source == name);
            })
            .Select(svc => ServiceMapper.ToSwarmService(svc, networks))
            .ToList();

        return Ok(filtered);
    }
}

public class CreateVolumeRequest
{
    public string VolumeName { get; set; } = "";
    public string? Driver { get; set; }
    public Dictionary<string, string>? Options { get; set; }
    public Dictionary<string, string>? Labels { get; set; }
}
