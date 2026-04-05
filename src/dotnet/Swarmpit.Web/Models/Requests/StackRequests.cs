namespace Swarmpit.Web.Models.Requests;

public class SaveStackFileRequest
{
    public string Compose { get; set; } = "";
}

public class RedeployStackRequest
{
    public string? Compose { get; set; }
}
