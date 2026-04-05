namespace Swarmpit.Core.Domain.Docker;

public class SwarmNetwork
{
    public string Id { get; set; } = "";
    public string NetworkName { get; set; } = "";
    public string Created { get; set; } = "";
    public string Scope { get; set; } = "";
    public string Driver { get; set; } = "";
    public bool Internal { get; set; }
    public List<NameValue> Options { get; set; } = [];
    public bool Attachable { get; set; }
    public bool Ingress { get; set; }
    public bool EnableIPv6 { get; set; }
    public Dictionary<string, string> Labels { get; set; } = new();
    public string? Stack { get; set; }
    public NetworkIpam Ipam { get; set; } = new();
}

public class NetworkIpam
{
    public string? Subnet { get; set; }
    public string? Gateway { get; set; }
}
