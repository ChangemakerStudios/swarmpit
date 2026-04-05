using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swarmpit.Core.Application.Docker;
using Swarmpit.Core.Domain.Docker;
using Swarmpit.Web.Models.Requests;

namespace Swarmpit.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/networks")]
public class NetworksController(INetworkRepository networks, IServiceRepository services) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List() => Ok(await networks.ListAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var network = await networks.GetAsync(id);
        return network != null ? Ok(network) : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateNetworkRequest request)
    {
        var id = await networks.CreateAsync(new CreateNetworkParams
        {
            NetworkName = request.NetworkName,
            Driver = request.Driver,
            Internal = request.Internal,
            Attachable = request.Attachable,
            Ingress = request.Ingress,
            EnableIPv6 = request.EnableIPv6,
            Options = request.Options,
            Ipam = request.Ipam != null
                ? new NetworkIpam { Subnet = request.Ipam.Subnet, Gateway = request.Ipam.Gateway }
                : null
        });
        return Ok(new { Id = id });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await networks.DeleteAsync(id);
        return NoContent();
    }

    [HttpGet("{id}/services")]
    public async Task<IActionResult> Services(string id) => Ok(await services.GetByNetworkAsync(id));
}
