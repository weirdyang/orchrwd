using OrchardCore.Data.Documents;

namespace OC.Workflow.Versioning.Implementation.Models
{
    public sealed class WorkflowVersioningDocument : Document
    {
        public Dictionary<string, WorkflowVersionInfo> Information { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
