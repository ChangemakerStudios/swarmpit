namespace Swarmpit.Api.Models;

public class SwarmStack
{
    public string StackName { get; set; } = "";
    public string State { get; set; } = "deployed"; // "deployed" or "inactive"
    public bool StackFile { get; set; }
    public SwarmStackStats Stats { get; set; } = new();
}

public class SwarmStackStats
{
    public int Services { get; set; }
    public int Networks { get; set; }
    public int Volumes { get; set; }
    public int Configs { get; set; }
    public int Secrets { get; set; }
}

public class SwarmStackDetail
{
    public string StackName { get; set; } = "";
    public string State { get; set; } = "deployed";
    public bool StackFile { get; set; }
    public SwarmStackStats Stats { get; set; } = new();
    public List<SwarmService> Services { get; set; } = [];
    public List<SwarmNetwork> Networks { get; set; } = [];
    public List<SwarmVolume> Volumes { get; set; } = [];
    public List<SwarmConfig> Configs { get; set; } = [];
    public List<SwarmSecret> Secrets { get; set; } = [];
}
