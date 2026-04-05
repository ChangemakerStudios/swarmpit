namespace Swarmpit.Api.Models.Requests;

public class CreateRegistryRequest
{
    public string Name { get; set; } = "";
    public string RegistryType { get; set; } = "";
    public string? Url { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool Public { get; set; }
    public string? Region { get; set; }
    public string? AccessKeyId { get; set; }
    public string? AccessKey { get; set; }
    public string? SpName { get; set; }
    public string? SpId { get; set; }
    public string? SpPassword { get; set; }
    public string? Token { get; set; }
    public bool? Hosted { get; set; }
    public string? GitlabUrl { get; set; }
    public bool? CustomApi { get; set; }
    public bool? WithAuth { get; set; }
}
