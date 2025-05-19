namespace OC.Workflow.Versioning.Implementation.Services
{
    public static class WorkflowVersionSettings
    {
        public static string DefaultFolder => "ArchivedWorkflows";
        public static string VersionFolder(string typeId)
        {
            return Path.Combine(Environment.CurrentDirectory, DefaultFolder, typeId);
        }
        public static string VersionFileName(string typeId, long? version = null)
        {
            version ??= DateTime.UtcNow.Ticks;

            return $"{typeId}.{version}.json";
        }
        public static string VersionCommentFileName(string typeId, long? version = null)
        {
            version ??= DateTime.UtcNow.Ticks;

            return $"{typeId}.{version}.comments.txt";
        }
    }
}
