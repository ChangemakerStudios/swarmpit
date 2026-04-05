namespace Swarmpit.Core.Application.Stacks;

public interface IStackFileRepository
{
    Task<StackFileDoc?> GetByNameAsync(string name);
    Task<List<StackFileDoc>> GetAllAsync();
    Task SaveAsync(string name, string compose);
    Task DeleteAsync(string name);
}

public class StackFileDoc
{
    public string Id { get; set; } = "";
    public string Rev { get; set; } = "";
    public string Name { get; set; } = "";
    public StackFileSpec? Spec { get; set; }
    public StackFileSpec? PreviousSpec { get; set; }
}

public class StackFileSpec
{
    public string Compose { get; set; } = "";
}
