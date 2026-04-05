using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swarmpit.Core.Application.Docker;
using Swarmpit.Core.Domain.Docker;
using Swarmpit.Web.Models.Requests;

namespace Swarmpit.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/services")]
public class ServicesController(IServiceRepository services, IComposeGeneratorService composeGenerator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List() => Ok(await services.ListAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var service = await services.GetAsync(id);
        return service != null ? Ok(service) : NotFound();
    }

    [HttpGet("{id}/tasks")]
    public async Task<IActionResult> Tasks(string id) => Ok(await services.GetTasksAsync(id));

    [HttpGet("{id}/networks")]
    public async Task<IActionResult> Networks(string id)
    {
        try
        {
            return Ok(await services.GetNetworksAsync(id));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await services.DeleteAsync(id);
        return NoContent();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateServiceRequest request)
    {
        var serviceId = await services.CreateAsync(MapToParams(request));
        return Ok(new { id = serviceId });
    }

    [HttpPost("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] CreateServiceRequest request)
    {
        try
        {
            await services.UpdateAsync(id, MapToParams(request));
            return Ok(new { id });
        }
        catch (Docker.DotNet.DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound();
        }
    }

    [HttpPost("{id}/redeploy")]
    public async Task<IActionResult> Redeploy(string id, [FromQuery] string? tag = null)
    {
        try
        {
            await services.RedeployAsync(id, tag);
            return Ok(new { id });
        }
        catch (Docker.DotNet.DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound();
        }
    }

    [HttpPost("{id}/rollback")]
    public async Task<IActionResult> Rollback(string id)
    {
        try
        {
            await services.RollbackAsync(id);
            return Ok(new { id });
        }
        catch (Docker.DotNet.DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/stop")]
    public async Task<IActionResult> Stop(string id)
    {
        try
        {
            await services.StopAsync(id);
            return Ok(new { id });
        }
        catch (Docker.DotNet.DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id}/logs")]
    public async Task<IActionResult> Logs(string id, [FromQuery] string? since = null)
    {
        try
        {
            var logs = await services.GetLogsAsync(id, since);
            return Ok(logs);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("{id}/compose")]
    public async Task<IActionResult> Compose(string id)
    {
        try
        {
            var yaml = await composeGenerator.GenerateServiceComposeAsync(id);
            return Content(yaml, "text/yaml");
        }
        catch (Docker.DotNet.DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound();
        }
    }

    private static CreateServiceParams MapToParams(CreateServiceRequest request) => new()
    {
        ServiceName = request.ServiceName,
        Image = request.Image,
        Mode = request.Mode,
        Replicas = request.Replicas,
        Command = request.Command,
        User = request.User,
        Dir = request.Dir,
        Tty = request.Tty,
        Ports = request.Ports?.Select(p => new ServicePort
        {
            ContainerPort = p.ContainerPort,
            HostPort = p.HostPort,
            Protocol = p.Protocol,
            Mode = p.Mode
        }).ToList(),
        Networks = request.Networks?.Select(n => new ServiceNetwork
        {
            Id = n.Id,
            NetworkName = n.NetworkName
        }).ToList(),
        Mounts = request.Mounts?.Select(m => new ServiceMount
        {
            Type = m.Type,
            Source = m.Source,
            Target = m.Target,
            ReadOnly = m.ReadOnly
        }).ToList(),
        Variables = request.Variables?.Select(v => new NameValue { Name = v.Name, Value = v.Value }).ToList(),
        Labels = request.Labels?.Select(l => new NameValue { Name = l.Name, Value = l.Value }).ToList(),
        Secrets = request.Secrets?.Select(s => new ServiceSecretRef
        {
            Id = s.Id,
            SecretName = s.SecretName,
            SecretTarget = s.SecretTarget
        }).ToList(),
        Configs = request.Configs?.Select(c => new ServiceConfigRef
        {
            Id = c.Id,
            ConfigName = c.ConfigName,
            ConfigTarget = c.ConfigTarget
        }).ToList(),
        Hosts = request.Hosts?.Select(h => new NameValue { Name = h.Name, Value = h.Value }).ToList(),
        Resources = request.Resources != null
            ? new ServiceResources
            {
                Reservation = request.Resources.Reservation != null
                    ? new ServiceResourceConfig { Cpu = request.Resources.Reservation.Cpu, Memory = request.Resources.Reservation.Memory }
                    : new(),
                Limit = request.Resources.Limit != null
                    ? new ServiceResourceConfig { Cpu = request.Resources.Limit.Cpu, Memory = request.Resources.Limit.Memory }
                    : new()
            }
            : null,
        Deployment = request.Deployment != null
            ? new ServiceDeployment
            {
                Update = request.Deployment.Update != null
                    ? new ServiceDeploymentConfig
                    {
                        Parallelism = request.Deployment.Update.Parallelism,
                        Delay = request.Deployment.Update.Delay,
                        FailureAction = request.Deployment.Update.FailureAction,
                        Monitor = request.Deployment.Update.Monitor,
                        Order = request.Deployment.Update.Order
                    }
                    : new(),
                Rollback = request.Deployment.Rollback != null
                    ? new ServiceDeploymentConfig
                    {
                        Parallelism = request.Deployment.Rollback.Parallelism,
                        Delay = request.Deployment.Rollback.Delay,
                        FailureAction = request.Deployment.Rollback.FailureAction,
                        Monitor = request.Deployment.Rollback.Monitor,
                        Order = request.Deployment.Rollback.Order
                    }
                    : new(),
                RestartPolicy = request.Deployment.RestartPolicy != null
                    ? new ServiceRestartPolicy
                    {
                        Condition = request.Deployment.RestartPolicy.Condition,
                        Delay = request.Deployment.RestartPolicy.Delay,
                        MaxAttempts = request.Deployment.RestartPolicy.MaxAttempts,
                        Window = request.Deployment.RestartPolicy.Window
                    }
                    : new(),
                Placement = request.Deployment.Placement != null
                    ? new ServicePlacement { Constraints = request.Deployment.Placement.Constraints ?? [] }
                    : new(),
                AutoRedeploy = request.Deployment.AutoRedeploy
            }
            : null,
        Logdriver = request.Logdriver != null
            ? new ServiceLogdriver
            {
                Name = request.Logdriver.Name,
                Opts = request.Logdriver.Opts?.Select(o => new NameValue { Name = o.Name, Value = o.Value }).ToList() ?? []
            }
            : null
    };
}
