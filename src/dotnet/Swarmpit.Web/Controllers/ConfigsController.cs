using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swarmpit.Core.Application.Docker;
using Swarmpit.Web.Models.Requests;

namespace Swarmpit.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/configs")]
public class ConfigsController(IConfigRepository configs, IServiceRepository services) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List() => Ok(await configs.ListAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var config = await configs.GetAsync(id);
        return config != null ? Ok(config) : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateConfigRequest request)
    {
        var id = await configs.CreateAsync(request.ConfigName, request.Data);
        return Ok(new { ID = id });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await configs.DeleteAsync(id);
        return NoContent();
    }

    [HttpGet("{id}/services")]
    public async Task<IActionResult> Services(string id) => Ok(await services.GetByConfigAsync(id));
}
