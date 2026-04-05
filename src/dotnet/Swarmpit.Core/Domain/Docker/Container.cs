namespace Swarmpit.Core.Domain.Docker;

public class Container
{
    public string Id { get; set; } = "";
    public string FullId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Image { get; set; } = "";
    public string ImageId { get; set; } = "";
    public string State { get; set; } = "";
    public string Status { get; set; } = "";
    public string Created { get; set; } = "";
    public List<ContainerPort> Ports { get; set; } = [];
    public Dictionary<string, string> Labels { get; set; } = new();
    public List<string> Networks { get; set; } = [];
    public List<ContainerMount> Mounts { get; set; } = [];
    public string Command { get; set; } = "";
    public string? Stack { get; set; }
}

public class ContainerPort
{
    public int PrivatePort { get; set; }
    public int? PublicPort { get; set; }
    public string Type { get; set; } = "tcp";
    public string? Ip { get; set; }
}

public class ContainerMount
{
    public string Type { get; set; } = "";
    public string Source { get; set; } = "";
    public string Destination { get; set; } = "";
    public bool ReadOnly { get; set; }
}

public class ContainerDetail : Container
{
    public List<string> Env { get; set; } = [];
    public string RestartPolicy { get; set; } = "";
    public string? Hostname { get; set; }
    public string? WorkingDir { get; set; }
    public string? User { get; set; }
    public string? Platform { get; set; }
    public string? Driver { get; set; }
}
