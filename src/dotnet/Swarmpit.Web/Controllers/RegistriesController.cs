using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swarmpit.Core.Application.Registries;
using Swarmpit.Core.Domain.Registries;
using Swarmpit.Web.Models.Requests;

namespace Swarmpit.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/registries")]
public class RegistriesController(IRegistryRepository registries) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? registryType = null)
    {
        List<Registry> list;

        if (!string.IsNullOrEmpty(registryType))
        {
            list = await registries.GetByTypeAsync(registryType);
        }
        else
        {
            list = await registries.GetAllAsync();
        }

        var sanitized = list.Select(SanitizeRegistry).ToList();
        return Ok(sanitized);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var registry = await registries.GetByIdAsync(id);
        if (registry == null) return NotFound();

        return Ok(SanitizeRegistry(registry));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRegistryRequest request)
    {
        var owner = User.Identity?.Name ?? "";

        var registry = new Registry
        {
            Name = request.Name,
            RegistryType = request.RegistryType,
            Url = request.Url ?? "",
            Username = request.Username,
            Password = request.Password,
            Public = request.Public,
            Owner = owner,
            Region = request.Region,
            AccessKeyId = request.AccessKeyId,
            AccessKey = request.AccessKey,
            SpName = request.SpName,
            SpId = request.SpId,
            SpPassword = request.SpPassword,
            Token = request.Token,
            Hosted = request.Hosted,
            GitlabUrl = request.GitlabUrl
        };

        var result = await registries.CreateAsync(registry);
        return Ok(new { id = result.Id });
    }

    [HttpPost("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] CreateRegistryRequest request)
    {
        var existing = await registries.GetByIdAsync(id);
        if (existing == null) return NotFound();

        var registry = new Registry
        {
            Name = request.Name,
            RegistryType = request.RegistryType,
            Url = request.Url ?? "",
            Username = request.Username,
            Password = request.Password,
            Public = request.Public,
            Owner = existing.Owner,
            Region = request.Region,
            AccessKeyId = request.AccessKeyId,
            AccessKey = request.AccessKey,
            SpName = request.SpName,
            SpId = request.SpId,
            SpPassword = request.SpPassword,
            Token = request.Token,
            Hosted = request.Hosted,
            GitlabUrl = request.GitlabUrl
        };

        await registries.UpdateAsync(id, existing.Rev, registry);
        return Ok(new { id });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var existing = await registries.GetByIdAsync(id);
        if (existing == null) return NotFound();

        await registries.DeleteAsync(id, existing.Rev);
        return NoContent();
    }

    [HttpGet("{id}/repositories")]
    public Task<IActionResult> Repositories(string id)
    {
        return Task.FromResult<IActionResult>(Ok(Array.Empty<object>()));
    }

    private static Registry SanitizeRegistry(Registry registry)
    {
        return new Registry
        {
            Id = registry.Id,
            Rev = registry.Rev,
            Name = registry.Name,
            RegistryType = registry.RegistryType,
            Url = registry.Url,
            Public = registry.Public,
            Owner = registry.Owner,
            Region = registry.Region,
            Hosted = registry.Hosted,
            GitlabUrl = registry.GitlabUrl,
            Username = registry.Username != null ? "***" : null,
            Password = null,
            AccessKeyId = registry.AccessKeyId != null ? "***" : null,
            AccessKey = null,
            SpName = registry.SpName,
            SpId = registry.SpId != null ? "***" : null,
            SpPassword = null,
            Token = null
        };
    }
}
