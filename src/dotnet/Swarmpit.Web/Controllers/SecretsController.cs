using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swarmpit.Core.Application.Docker;
using Swarmpit.Web.Models.Requests;

using DockerSecretRepository = Swarmpit.Core.Application.Docker.ISecretRepository;

namespace Swarmpit.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/secrets")]
public class SecretsController(DockerSecretRepository secrets, IServiceRepository services) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List() => Ok(await secrets.ListAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var secret = await secrets.GetAsync(id);
        return secret != null ? Ok(secret) : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSecretRequest request)
    {
        var id = await secrets.CreateAsync(request.SecretName, request.Data);
        return Ok(new { ID = id });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await secrets.DeleteAsync(id);
        return NoContent();
    }

    [HttpGet("{id}/services")]
    public async Task<IActionResult> Services(string id) => Ok(await services.GetBySecretAsync(id));
}
