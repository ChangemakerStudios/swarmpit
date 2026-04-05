using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swarmpit.Core.Application.Docker;
using Swarmpit.Web.Models.Requests;

namespace Swarmpit.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/volumes")]
public class VolumesController(IVolumeRepository volumes, IServiceRepository services) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List() => Ok(await volumes.ListAsync());

    [HttpGet("{name}")]
    public async Task<IActionResult> Get(string name)
    {
        var volume = await volumes.GetAsync(name);
        return volume != null ? Ok(volume) : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateVolumeRequest request)
    {
        var volume = await volumes.CreateAsync(new CreateVolumeParams
        {
            VolumeName = request.VolumeName,
            Driver = request.Driver,
            Options = request.Options,
            Labels = request.Labels
        });
        return Ok(volume);
    }

    [HttpDelete("{name}")]
    public async Task<IActionResult> Delete(string name)
    {
        await volumes.DeleteAsync(name);
        return NoContent();
    }

    [HttpGet("{name}/services")]
    public async Task<IActionResult> Services(string name) => Ok(await services.GetByVolumeAsync(name));
}
