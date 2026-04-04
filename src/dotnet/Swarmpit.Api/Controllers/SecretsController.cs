using Docker.DotNet.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swarmpit.Api.Docker;
using Swarmpit.Api.Docker.Mappers;

namespace Swarmpit.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/secrets")]
public class SecretsController : ControllerBase
{
    private readonly DockerClientFactory _docker;

    public SecretsController(DockerClientFactory docker)
    {
        _docker = docker;
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var client = _docker.GetClient();
        var secrets = await client.Secrets.ListAsync();
        var mapped = secrets.Select(SecretMapper.ToSwarmSecret).ToList();
        return Ok(mapped);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var client = _docker.GetClient();
        try
        {
            var secret = await client.Secrets.InspectAsync(id);
            return Ok(SecretMapper.ToSwarmSecret(secret));
        }
        catch (global::Docker.DotNet.DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound();
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSecretRequest request)
    {
        var client = _docker.GetClient();

        var spec = new SecretSpec
        {
            Name = request.SecretName,
            Data = Convert.FromBase64String(request.Data)
        };

        var response = await client.Secrets.CreateAsync(spec);
        return Ok(new { response.ID });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var client = _docker.GetClient();
        await client.Secrets.DeleteAsync(id);
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
                var secrets = svc.Spec?.TaskTemplate?.ContainerSpec?.Secrets;
                return secrets != null && secrets.Any(s => s.SecretID == id);
            })
            .Select(svc => ServiceMapper.ToSwarmService(svc, networks))
            .ToList();

        return Ok(filtered);
    }
}

public class CreateSecretRequest
{
    public string SecretName { get; set; } = "";
    public string Data { get; set; } = "";
}
