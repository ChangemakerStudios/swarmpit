using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swarmpit.Core.Application.Docker;

namespace Swarmpit.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/tasks")]
public class TasksController(ITaskRepository tasks) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List() => Ok(await tasks.ListAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var task = await tasks.GetAsync(id);
        return task != null ? Ok(task) : NotFound();
    }
}
