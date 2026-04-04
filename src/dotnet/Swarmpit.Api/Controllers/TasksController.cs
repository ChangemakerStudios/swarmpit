using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swarmpit.Api.Docker;
using Swarmpit.Api.Docker.Mappers;

namespace Swarmpit.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/tasks")]
public class TasksController : ControllerBase
{
    private readonly DockerClientFactory _docker;

    public TasksController(DockerClientFactory docker)
    {
        _docker = docker;
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var client = _docker.GetClient();

        var tasks = await client.Tasks.ListAsync();
        var services = await client.Swarm.ListServicesAsync();
        var nodes = await client.Swarm.ListNodesAsync();

        var serviceNames = services.ToDictionary(
            s => s.ID ?? "",
            s => s.Spec?.Name ?? "");

        var nodeNames = nodes.ToDictionary(
            n => n.ID ?? "",
            n => n.Description?.Hostname ?? "");

        var mapped = tasks
            .Select(t => TaskMapper.ToSwarmTask(t, serviceNames, nodeNames))
            .ToList();

        return Ok(mapped);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var client = _docker.GetClient();

        global::Docker.DotNet.Models.TaskResponse task;
        try
        {
            task = await client.Tasks.InspectAsync(id);
        }
        catch (global::Docker.DotNet.DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound();
        }

        var services = await client.Swarm.ListServicesAsync();
        var nodes = await client.Swarm.ListNodesAsync();

        var serviceNames = services.ToDictionary(
            s => s.ID ?? "",
            s => s.Spec?.Name ?? "");

        var nodeNames = nodes.ToDictionary(
            n => n.ID ?? "",
            n => n.Description?.Hostname ?? "");

        return Ok(TaskMapper.ToSwarmTask(task, serviceNames, nodeNames));
    }
}
