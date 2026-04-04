namespace Swarmpit.Api.Models;

public class SwarmNode
{
    public string Id { get; set; } = "";
    public long Version { get; set; }
    public string NodeName { get; set; } = "";
    public string Role { get; set; } = "";
    public string Availability { get; set; } = "";
    public List<NameValue> Labels { get; set; } = [];
    public string State { get; set; } = "";
    public string? Address { get; set; }
    public string? Engine { get; set; }
    public string? Arch { get; set; }
    public string? Os { get; set; }
    public NodeResources Resources { get; set; } = new();
    public NodePlugins Plugins { get; set; } = new();
    public bool? Leader { get; set; }
}

public class NodeResources
{
    public double Cpu { get; set; }
    public double Memory { get; set; }
}

public class NodePlugins
{
    public List<string> Networks { get; set; } = [];
    public List<string> Volumes { get; set; } = [];
}

public class NameValue
{
    public string Name { get; set; } = "";
    public string Value { get; set; } = "";
}
