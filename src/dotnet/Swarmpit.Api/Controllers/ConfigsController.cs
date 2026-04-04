using Docker.DotNet.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swarmpit.Api.Docker;
using Swarmpit.Api.Docker.Mappers;

namespace Swarmpit.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/configs")]
public class ConfigsController : ControllerBase
{
    private readonly DockerClientFactory _docker;

    public ConfigsController(DockerClientFactory docker)
    {
        _docker = docker;
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var client = _docker.GetClient();
        var configs = await client.Configs.ListConfigsAsync();
        var mapped = configs.Select(ConfigMapper.ToSwarmConfig).ToList();
        return Ok(mapped);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var client = _docker.GetClient();
        try
        {
            var config = await client.Configs.InspectConfigAsync(id);
            return Ok(ConfigMapper.ToSwarmConfig(config));
        }
        catch (global::Docker.DotNet.DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound();
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateConfigRequest request)
    {
        var client = _docker.GetClient();

        var body = new SwarmCreateConfigParameters
        {
            Config = new SwarmConfigSpec
            {
                Name = request.ConfigName,
                Data = Convert.FromBase64String(request.Data)
            }
        };

        var response = await client.Configs.CreateConfigAsync(body);
        return Ok(new { response.ID });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var client = _docker.GetClient();
        await client.Configs.RemoveConfigAsync(id);
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
                var configs = svc.Spec?.TaskTemplate?.ContainerSpec?.Configs;
                return configs != null && configs.Any(c => c.ConfigID == id);
            })
            .Select(svc => ServiceMapper.ToSwarmService(svc, networks))
            .ToList();

        return Ok(filtered);
    }
}

public class CreateConfigRequest
{
    public string ConfigName { get; set; } = "";
    public string Data { get; set; } = "";
}
