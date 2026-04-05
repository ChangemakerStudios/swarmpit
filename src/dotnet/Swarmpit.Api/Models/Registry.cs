namespace Swarmpit.Api.Models;

public class Registry
{
    public string Id { get; set; } = "";
    public string Rev { get; set; } = "";
    public string Name { get; set; } = "";
    public string RegistryType { get; set; } = ""; // v2, dockerhub, ecr, acr, gitlab, ghcr
    public string Url { get; set; } = "";
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool Public { get; set; }
    public string Owner { get; set; } = "";
    // ECR specific
    public string? Region { get; set; }
    public string? AccessKeyId { get; set; }
    public string? AccessKey { get; set; }
    // ACR specific
    public string? SpName { get; set; }
    public string? SpId { get; set; }
    public string? SpPassword { get; set; }
    // GitLab specific
    public string? Token { get; set; }
    public bool? Hosted { get; set; }
    public string? GitlabUrl { get; set; }
}
