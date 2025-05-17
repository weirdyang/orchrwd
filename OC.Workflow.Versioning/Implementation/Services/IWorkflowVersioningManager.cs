using OC.Workflow.Versioning.Implementation.Models;

namespace OC.Workflow.Versioning.Implementation.Services
{
    public interface IWorkflowVersioningManager
    {
        Task<WorkflowVersionInfo?> AddWorkflowVersionAsync(string workflowTypeId);
        Task<WorkflowVersionInfo?> GetVersionInfoAsync(string workflowTypeId);
        Task<WorkflowVersionInfo> UpdateRestoreAsync(string workflowTypeId, long version);
    }
}
