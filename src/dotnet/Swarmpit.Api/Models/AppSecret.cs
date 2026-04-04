namespace Swarmpit.Api.Models;

public class AppSecret
{
    public string? Id { get; set; }
    public string? Rev { get; set; }
    public string Type { get; set; } = "secret";
    public string Secret { get; set; } = "";
}
