namespace Swarmpit.Core.Application.Users;

public interface ISecretRepository
{
    Task<string> GetOrCreateSecretAsync();
}
