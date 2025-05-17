using OrchardCore.Workflows.Models;

namespace OC.Workflow.Versioning.Implementation.Models
{
    public class WorkflowVersionViewModel
    {
        public WorkflowType WorkflowType { get; set; } = new();
        public int TotalVersions { get; set; } = 0;
    }
}
