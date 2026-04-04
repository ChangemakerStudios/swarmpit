namespace Swarmpit.Api;

public static class AppConstants
{
    public const string AppName = "swarmpit";

    public const string AppIssuer = "swarmpit";

    public const string ApiIssuer = "swarmpit-api";

    public const string CouchDbName = "swarmpit";

    public static class DockerLabels
    {
        public const string StackNamespace = "com.docker.stack.namespace";

        public const string DockerLabelPrefix = "com.docker.";

        public const string SwarmpitLabelPrefix = "swarmpit.";

        public const string Immutable = "swarmpit.service.immutable";

        public const string Agent = "swarmpit.agent";

        public const string AutoRedeploy = "swarmpit.service.deployment.autoredeploy";

        public const string LinkPrefix = "swarmpit.service.link.";
    }
}