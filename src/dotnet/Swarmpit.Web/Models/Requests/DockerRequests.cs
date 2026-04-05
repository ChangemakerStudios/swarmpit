namespace Swarmpit.Web.Models.Requests;

public class CreateSecretRequest
{
    public string SecretName { get; set; } = "";
    public string Data { get; set; } = "";
}

public class CreateConfigRequest
{
    public string ConfigName { get; set; } = "";
    public string Data { get; set; } = "";
}

public class CreateNetworkRequest
{
    public string NetworkName { get; set; } = "";
    public string? Driver { get; set; }
    public bool Internal { get; set; }
    public bool Attachable { get; set; }
    public bool Ingress { get; set; }
    public bool EnableIPv6 { get; set; }
    public NetworkIpamRequest? Ipam { get; set; }
    public Dictionary<string, string>? Options { get; set; }
}

public class NetworkIpamRequest
{
    public string? Subnet { get; set; }
    public string? Gateway { get; set; }
}

public class CreateVolumeRequest
{
    public string VolumeName { get; set; } = "";
    public string? Driver { get; set; }
    public Dictionary<string, string>? Options { get; set; }
    public Dictionary<string, string>? Labels { get; set; }
}
