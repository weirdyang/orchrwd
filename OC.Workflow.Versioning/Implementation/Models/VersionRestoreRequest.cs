namespace OC.Workflow.Versioning.Implementation.Models
{
    public class VersionRestoreRequest
    {
        public string WorkflowTypeId { get; set; } = string.Empty;
        public DateTime RequestTimestamp { get; set; } = DateTime.UtcNow;
        public long Version { get; set; } = DateTime.UtcNow.Ticks;
    }
}
