using Docker.DotNet.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swarmpit.Api.Docker;
using Swarmpit.Api.Docker.Mappers;

namespace Swarmpit.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/services")]
public class ServicesController : ControllerBase
{
    private readonly DockerClientFactory _docker;

    public ServicesController(DockerClientFactory docker)
    {
        _docker = docker;
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var client = _docker.GetClient();

        var services = await client.Swarm.ListServicesAsync();
        var networks = await client.Networks.ListNetworksAsync();
        var tasks = await client.Tasks.ListAsync();
        var nodes = await client.Swarm.ListNodesAsync();

        var tasksByService = tasks
            .GroupBy(t => t.ServiceID ?? "")
            .ToDictionary(g => g.Key, g => g.ToList());

        var nodeCount = nodes.Count(n =>
            n.Status?.State?.ToString()?.ToLower() == "ready"
            && n.Spec?.Availability?.ToLower() == "active");

        var mapped = services.Select(svc =>
        {
            var service = ServiceMapper.ToSwarmService(svc, networks);
            var serviceTasks = tasksByService.GetValueOrDefault(svc.ID ?? "", []);
            ComputeStatus(service, serviceTasks, nodeCount);
            return service;
        }).ToList();

        return Ok(mapped);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var client = _docker.GetClient();

        global::Docker.DotNet.Models.SwarmService svc;
        try
        {
            svc = await client.Swarm.InspectServiceAsync(id);
        }
        catch (global::Docker.DotNet.DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound();
        }

        var networks = await client.Networks.ListNetworksAsync();
        var tasks = await client.Tasks.ListAsync();
        var nodes = await client.Swarm.ListNodesAsync();

        var serviceTasks = tasks.Where(t => t.ServiceID == id).ToList();

        var nodeCount = nodes.Count(n =>
            n.Status?.State?.ToString()?.ToLower() == "ready"
            && n.Spec?.Availability?.ToLower() == "active");

        var service = ServiceMapper.ToSwarmService(svc, networks);
        ComputeStatus(service, serviceTasks, nodeCount);

        return Ok(service);
    }

    [HttpGet("{id}/tasks")]
    public async Task<IActionResult> Tasks(string id)
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
            .Where(t => t.ServiceID == id)
            .Select(t => TaskMapper.ToSwarmTask(t, serviceNames, nodeNames))
            .ToList();

        return Ok(mapped);
    }

    [HttpGet("{id}/networks")]
    public async Task<IActionResult> Networks(string id)
    {
        var client = _docker.GetClient();

        global::Docker.DotNet.Models.SwarmService svc;
        try
        {
            svc = await client.Swarm.InspectServiceAsync(id);
        }
        catch (global::Docker.DotNet.DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound();
        }

        var networkIds = new List<string>();

        var taskNetworks = svc.Spec?.TaskTemplate?.Networks;
        if (taskNetworks != null)
        {
            networkIds.AddRange(taskNetworks
                .Select(n => n.Target)
                .Where(n => n != null)!);
        }

        var specNetworks = svc.Spec?.Networks;
        if (specNetworks != null)
        {
            foreach (var n in specNetworks)
            {
                if (n.Target != null && !networkIds.Contains(n.Target))
                    networkIds.Add(n.Target);
            }
        }

        var mapped = new List<Models.SwarmNetwork>();
        foreach (var networkId in networkIds)
        {
            try
            {
                var network = await client.Networks.InspectNetworkAsync(networkId);
                mapped.Add(NetworkMapper.ToSwarmNetwork(network));
            }
            catch (global::Docker.DotNet.DockerApiException)
            {
                // Network may have been removed; skip it
            }
        }

        return Ok(mapped);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var client = _docker.GetClient();
        await client.Swarm.RemoveServiceAsync(id);
        return NoContent();
    }

    private static void ComputeStatus(
        Models.SwarmService service,
        IList<TaskResponse> tasks,
        int nodeCount)
    {
        var desiredRunning = tasks
            .Where(t => t.DesiredState.ToString().ToLower() == "running")
            .ToList();

        var runningCount = desiredRunning
            .Count(t => t.Status?.State.ToString().ToLower() == "running");

        var total = service.Mode == "global"
            ? nodeCount
            : service.Replicas ?? 0;

        service.Status.Tasks = new Models.ServiceTaskStatus
        {
            Running = runningCount,
            Total = total
        };

        if (runningCount == total && total > 0)
            service.State = "running";
        else if (runningCount == 0)
            service.State = "not running";
        else
            service.State = "partly running";
    }
}
