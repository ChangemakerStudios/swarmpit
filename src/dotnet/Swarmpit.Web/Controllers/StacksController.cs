using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swarmpit.Core.Application.Docker;
using Swarmpit.Core.Application.Stacks;
using Swarmpit.Core.Domain.Docker;
using Swarmpit.Web.Models.Requests;

using DockerSecretRepository = Swarmpit.Core.Application.Docker.ISecretRepository;

namespace Swarmpit.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/stacks")]
public class StacksController(
    IServiceRepository services,
    INetworkRepository networks,
    IVolumeRepository volumes,
    DockerSecretRepository secrets,
    IConfigRepository configs,
    IStackFileRepository stackFiles,
    IStackDeployService stackDeploy,
    IComposeGeneratorService composeGenerator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var allServices = await services.ListAsync();
        var allNetworks = await networks.ListAsync();
        var allVolumes = await volumes.ListAsync();
        var allConfigs = await configs.ListAsync();
        var allSecrets = await secrets.ListAsync();
        var stackFileDocs = await stackFiles.GetAllAsync();

        var stacks = new Dictionary<string, SwarmStackStats>();

        foreach (var svc in allServices)
        {
            if (!string.IsNullOrEmpty(svc.Stack))
            {
                if (!stacks.ContainsKey(svc.Stack))
                    stacks[svc.Stack] = new SwarmStackStats();
                stacks[svc.Stack].Services++;
            }
        }

        foreach (var net in allNetworks)
        {
            if (!string.IsNullOrEmpty(net.Stack))
            {
                if (!stacks.ContainsKey(net.Stack))
                    stacks[net.Stack] = new SwarmStackStats();
                stacks[net.Stack].Networks++;
            }
        }

        foreach (var vol in allVolumes)
        {
            if (!string.IsNullOrEmpty(vol.Stack))
            {
                if (!stacks.ContainsKey(vol.Stack))
                    stacks[vol.Stack] = new SwarmStackStats();
                stacks[vol.Stack].Volumes++;
            }
        }

        foreach (var cfg in allConfigs)
        {
            // Configs don't have a Stack property in the domain model - check labels
            // The stack info is already parsed in the mapper, but SwarmConfig doesn't have Stack
            // We need to count by checking if config belongs to a stack via naming convention
        }

        foreach (var sec in allSecrets)
        {
            // Same limitation as configs
        }

        var stackFileNames = stackFileDocs.Select(sf => sf.Name).ToHashSet();

        var result = new List<SwarmStack>();

        foreach (var (name, stats) in stacks)
        {
            result.Add(new SwarmStack
            {
                StackName = name,
                State = "deployed",
                StackFile = stackFileNames.Contains(name),
                Stats = stats
            });
        }

        foreach (var sf in stackFileDocs)
        {
            if (!stacks.ContainsKey(sf.Name))
            {
                result.Add(new SwarmStack
                {
                    StackName = sf.Name,
                    State = "inactive",
                    StackFile = true,
                    Stats = new SwarmStackStats()
                });
            }
        }

        return Ok(result.OrderBy(s => s.StackName));
    }

    [HttpGet("{name}")]
    public async Task<IActionResult> Get(string name)
    {
        var stackServices = await services.GetByStackAsync(name);
        var stackNetworks = await networks.GetByStackAsync(name);
        var stackVolumes = await volumes.GetByStackAsync(name);
        var stackConfigs = await configs.GetByStackAsync(name);
        var stackSecrets = await secrets.GetByStackAsync(name);

        if (stackServices.Count == 0 && stackNetworks.Count == 0 && stackVolumes.Count == 0
            && stackConfigs.Count == 0 && stackSecrets.Count == 0)
        {
            var sf = await stackFiles.GetByNameAsync(name);
            if (sf == null) return NotFound();

            return Ok(new SwarmStackDetail
            {
                StackName = name,
                State = "inactive",
                StackFile = true,
                Stats = new SwarmStackStats()
            });
        }

        var stackFile = await stackFiles.GetByNameAsync(name);

        return Ok(new SwarmStackDetail
        {
            StackName = name,
            State = "deployed",
            StackFile = stackFile != null,
            Stats = new SwarmStackStats
            {
                Services = stackServices.Count,
                Networks = stackNetworks.Count,
                Volumes = stackVolumes.Count,
                Configs = stackConfigs.Count,
                Secrets = stackSecrets.Count
            },
            Services = stackServices,
            Networks = stackNetworks,
            Volumes = stackVolumes,
            Configs = stackConfigs,
            Secrets = stackSecrets
        });
    }

    [HttpGet("{name}/services")]
    public async Task<IActionResult> Services(string name) => Ok(await services.GetByStackAsync(name));

    [HttpGet("{name}/networks")]
    public async Task<IActionResult> Networks(string name) => Ok(await networks.GetByStackAsync(name));

    [HttpGet("{name}/volumes")]
    public async Task<IActionResult> Volumes(string name) => Ok(await volumes.GetByStackAsync(name));

    [HttpGet("{name}/configs")]
    public async Task<IActionResult> Configs(string name) => Ok(await configs.GetByStackAsync(name));

    [HttpGet("{name}/secrets")]
    public async Task<IActionResult> Secrets(string name) => Ok(await secrets.GetByStackAsync(name));

    [HttpGet("{name}/file")]
    public async Task<IActionResult> GetFile(string name)
    {
        var stackFile = await stackFiles.GetByNameAsync(name);
        if (stackFile == null) return NotFound();

        return Ok(new
        {
            spec = stackFile.Spec != null ? new { compose = stackFile.Spec.Compose } : null,
            previousSpec = stackFile.PreviousSpec != null ? new { compose = stackFile.PreviousSpec.Compose } : null
        });
    }

    [HttpPost("{name}/file")]
    public async Task<IActionResult> SaveFile(string name, [FromBody] SaveStackFileRequest request)
    {
        await stackFiles.SaveAsync(name, request.Compose);
        return Ok(new { message = "Stackfile saved" });
    }

    [HttpGet("{name}/compose")]
    public async Task<IActionResult> Compose(string name)
    {
        var yaml = await composeGenerator.GenerateStackComposeAsync(name);
        return Content(yaml, "text/yaml");
    }

    [HttpDelete("{name}")]
    public async Task<IActionResult> Delete(string name)
    {
        var stackServices = await services.GetByStackAsync(name);

        foreach (var svc in stackServices)
        {
            await services.DeleteAsync(svc.Id);
        }

        await stackFiles.DeleteAsync(name);

        return Ok(new { message = $"Stack '{name}' removed ({stackServices.Count} services deleted)" });
    }

    [HttpPost("{name}/deploy")]
    public async Task<IActionResult> Deploy(string name, [FromBody] SaveStackFileRequest request)
    {
        try
        {
            var result = await stackDeploy.DeployAsync(name, request.Compose);
            return Ok(new { message = $"Stack '{name}' deployed", details = result.Details });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = $"Failed to parse compose: {ex.Message}" });
        }
    }

    [HttpPost("{name}/redeploy")]
    public async Task<IActionResult> Redeploy(string name)
    {
        var stackFile = await stackFiles.GetByNameAsync(name);
        if (stackFile?.Spec == null)
            return BadRequest(new { error = "No stackfile found to redeploy" });

        try
        {
            var result = await stackDeploy.DeployAsync(name, stackFile.Spec.Compose);
            return Ok(new { message = $"Stack '{name}' deployed", details = result.Details });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
