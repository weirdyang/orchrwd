namespace OC.Workflow.Versioning.Implementation.Models
{
    public class WorkflowVersionInfo
    {
        public string WorkflowTypeId { get; set; } = string.Empty;
        public long CurrentVersion { get; set; }
        public long? PreviousVersion { get; set; }
        public DateTime? LastRestoredTime { get; set; }
    }

    public class VersionComment
    {
        public string Version { get; set; }
        public string? Comment { get; set; }
    }
}
