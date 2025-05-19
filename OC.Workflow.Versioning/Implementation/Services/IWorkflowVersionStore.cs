using OrchardCore.Workflows.Models;

namespace OC.Workflow.Versioning.Implementation.Services
{
    public interface IWorkflowVersionStore
    {
        Task PersistAsync(string typeId, long version);
        Task<string> RetrieveActiveWorkflowTypeJsonAsync(string typeId);
        Task<WorkflowType> RetrieveVersionedWorkflowTypeAsync(string typeId, long version);
        Task<string> RetrieveVersionedWorkflowTypeJsonAsync(string typeId, long version);
        Task PersistCommentAsync(string typeId, long version, string comment);
    }
}
