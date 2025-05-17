namespace OC.Workflow.Versioning.Implementation.Models
{
    public class VersionInfo
    {
        public long? Version { get; set; }
        public DateTime CreatedUtc => Version is null ? DateTime.UtcNow : new DateTime(Version.Value);
        public string WorkflowTypeId { get; set; } = string.Empty;

        public string Json { get; set; } = string.Empty;
    }
}
