using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swarmpit.Core.Application.Docker;

namespace Swarmpit.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/nodes")]
public class NodesController(INodeRepository nodes) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List() => Ok(await nodes.ListAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var node = await nodes.GetAsync(id);
        return node != null ? Ok(node) : NotFound();
    }

    [HttpGet("{id}/tasks")]
    public async Task<IActionResult> Tasks(string id) => Ok(await nodes.GetTasksAsync(id));
}
