using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swarmpit.Core.Application.Docker;

namespace Swarmpit.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/containers")]
public class ContainersController(IContainerRepository containers) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool all = true)
        => Ok(await containers.ListAsync(all));

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var container = await containers.GetAsync(id);
        return container != null ? Ok(container) : NotFound();
    }

    [HttpPost("{id}/start")]
    public async Task<IActionResult> Start(string id)
    {
        await containers.StartAsync(id);
        return Ok(new { id });
    }

    [HttpPost("{id}/stop")]
    public async Task<IActionResult> Stop(string id)
    {
        await containers.StopAsync(id);
        return Ok(new { id });
    }

    [HttpPost("{id}/restart")]
    public async Task<IActionResult> Restart(string id)
    {
        await containers.RestartAsync(id);
        return Ok(new { id });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Remove(string id, [FromQuery] bool force = false)
    {
        await containers.RemoveAsync(id, force);
        return NoContent();
    }

    [HttpGet("{id}/logs")]
    public async Task<IActionResult> Logs(string id, [FromQuery] string? since = null)
    {
        var logs = await containers.GetLogsAsync(id, since);
        return Ok(logs);
    }
}
