using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swarmpit.Api.Docker;
using Swarmpit.Api.Docker.Mappers;

namespace Swarmpit.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/nodes")]
public class NodesController : ControllerBase
{
    private readonly DockerClientFactory _docker;

    public NodesController(DockerClientFactory docker)
    {
        _docker = docker;
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var client = _docker.GetClient();
        var nodes = await client.Swarm.ListNodesAsync();
        var mapped = nodes.Select(NodeMapper.ToSwarmNode).ToList();
        return Ok(mapped);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var client = _docker.GetClient();
        var node = await client.Swarm.InspectNodeAsync(id);
        return Ok(NodeMapper.ToSwarmNode(node));
    }

    [HttpGet("{id}/tasks")]
    public async Task<IActionResult> Tasks(string id)
    {
        var client = _docker.GetClient();
        var tasks = await client.Tasks.ListAsync();
        var nodeTasks = tasks
            .Where(t => t.NodeID == id)
            .Select(t => new
            {
                t.ID,
                ServiceId = t.ServiceID,
                NodeId = t.NodeID,
                State = t.Status?.State.ToString(),
                DesiredState = t.DesiredState.ToString(),
                Image = t.Spec?.ContainerSpec?.Image,
                CreatedAt = t.CreatedAt
            })
            .ToList();
        return Ok(nodeTasks);
    }
}
