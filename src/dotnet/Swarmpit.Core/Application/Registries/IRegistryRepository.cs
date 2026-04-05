using Swarmpit.Core.Domain.Registries;

namespace Swarmpit.Core.Application.Registries;

public interface IRegistryRepository
{
    Task<List<Registry>> GetAllAsync();
    Task<List<Registry>> GetByTypeAsync(string registryType);
    Task<Registry?> GetByIdAsync(string id);
    Task<RegistryCreateResult> CreateAsync(Registry registry);
    Task UpdateAsync(string id, string rev, Registry registry);
    Task DeleteAsync(string id, string rev);
}

public class RegistryCreateResult
{
    public string Id { get; set; } = "";
    public string Rev { get; set; } = "";
}
