namespace OC.Workflow.Versioning.Implementation.Models
{
    public class WorkflowVersionInfo
    {
        public string WorkflowTypeId { get; set; } = string.Empty;
        public long CurrentVersion { get; set; }
        public long? PreviousVersion { get; set; }
        public DateTime? LastRestoredTime { get; set; }
    }
}
