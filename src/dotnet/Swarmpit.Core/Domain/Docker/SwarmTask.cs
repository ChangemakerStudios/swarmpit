namespace Swarmpit.Core.Domain.Docker;

public class SwarmTask
{
    public string Id { get; set; } = "";
    public string TaskName { get; set; } = "";
    public long Version { get; set; }
    public string? CreatedAt { get; set; }
    public string? UpdatedAt { get; set; }
    public TaskRepository Repository { get; set; } = new();
    public string State { get; set; } = "";
    public TaskStatus Status { get; set; } = new();
    public string DesiredState { get; set; } = "";
    public string ServiceName { get; set; } = "";
    public string NodeId { get; set; } = "";
    public string NodeName { get; set; } = "";
    public TaskResources Resources { get; set; } = new();
}

public class TaskRepository
{
    public string Image { get; set; } = "";
    public string? ImageDigest { get; set; }
}

public class TaskStatus
{
    public string? Error { get; set; }
}

public class TaskResources
{
    public TaskResourceConfig Reservation { get; set; } = new();
    public TaskResourceConfig Limit { get; set; } = new();
}

public class TaskResourceConfig
{
    public double Cpu { get; set; }
    public double Memory { get; set; }
}
