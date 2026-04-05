namespace Swarmpit.Core.Application.Docker;

public interface IStackDeployService
{
    Task<StackDeployResult> DeployAsync(string stackName, string composeYaml);
}

public class StackDeployResult
{
    public string StackName { get; set; } = "";
    public List<string> Details { get; set; } = [];
    public bool HasFailures => Details.Any(d => d.StartsWith("Failed"));
}
