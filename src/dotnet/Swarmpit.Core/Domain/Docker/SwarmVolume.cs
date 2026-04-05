namespace Swarmpit.Core.Domain.Docker;

public class SwarmVolume
{
    public string Id { get; set; } = "";

    public string VolumeName { get; set; } = "";

    public string Driver { get; set; } = "";

    public string Scope { get; set; } = "";

    public Dictionary<string, string> Labels { get; set; } = new();

    public string? Stack { get; set; }

    public List<NameValue> Options { get; set; } = [];

    public string? Mountpoint { get; set; }
}
