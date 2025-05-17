using OrchardCore.Workflows.Models;

namespace OC.Workflow.Versioning.Implementation.Models
{
    public class WorkflowVersionDetails
    {
        public string ActiveJson { get; set; } = string.Empty;
        public WorkflowType WorkflowType { get; set; } = new();
        public WorkflowVersionInfo VersionInfo { get; set; } = new();
        public List<VersionInfo> Versions { get; set; } = new List<VersionInfo>();

    }
}
