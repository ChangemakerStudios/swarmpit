using Docker.DotNet.Models;
using Swarmpit.Api.Models;
using TaskStatus = Swarmpit.Api.Models.TaskStatus;

namespace Swarmpit.Api.Docker.Mappers;

public static class TaskMapper
{
    public static SwarmTask ToSwarmTask(
        TaskResponse task,
        IDictionary<string, string> serviceNames,
        IDictionary<string, string> nodeNames)
    {
        var serviceId = task.ServiceID ?? "";
        var nodeId = task.NodeID ?? "";

        serviceNames.TryGetValue(serviceId, out var serviceName);
        nodeNames.TryGetValue(nodeId, out var nodeName);

        var taskName = BuildTaskName(task, serviceName ?? serviceId);

        var imageRaw = task.Spec?.ContainerSpec?.Image ?? "";
        var (image, imageDigest) = ParseImage(imageRaw);

        var resources = task.Spec?.Resources;
        var reservation = resources?.Reservations;
        var limit = resources?.Limits;

        return new SwarmTask
        {
            Id = task.ID ?? "",
            TaskName = taskName,
            Version = (long)(task.Version?.Index ?? 0),
            CreatedAt = task.CreatedAt.ToString("o"),
            UpdatedAt = task.UpdatedAt.ToString("o"),
            Repository = new TaskRepository
            {
                Image = image,
                ImageDigest = imageDigest
            },
            State = task.Status?.State.ToString() ?? "",

            Status = new TaskStatus
            {
                Error = task.Status?.Err
            },
            DesiredState = task.DesiredState.ToString(),
            ServiceName = serviceName ?? "",
            NodeId = nodeId,
            NodeName = nodeName ?? "",
            Resources = new TaskResources
            {
                Reservation = new TaskResourceConfig
                {
                    Cpu = (reservation?.NanoCPUs ?? 0) / 1_000_000_000.0,
                    Memory = (reservation?.MemoryBytes ?? 0) / (1024.0 * 1024.0)
                },
                Limit = new TaskResourceConfig
                {
                    Cpu = (limit?.NanoCPUs ?? 0) / 1_000_000_000.0,
                    Memory = (limit?.MemoryBytes ?? 0) / (1024.0 * 1024.0)
                }
            }
        };
    }

    private static string BuildTaskName(TaskResponse task, string serviceName)
    {
        if (task.Slot > 0)
        {
            return $"{serviceName}.{task.Slot}";
        }

        return $"{serviceName}.{task.NodeID ?? ""}";
    }

    private static (string image, string? digest) ParseImage(string imageRaw)
    {
        var atIndex = imageRaw.IndexOf('@');
        if (atIndex >= 0)
        {
            return (imageRaw[..atIndex], imageRaw[(atIndex + 1)..]);
        }

        return (imageRaw, null);
    }
}
