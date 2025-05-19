using OC.Workflow.Versioning.Implementation.Models;
using OrchardCore.Documents;

namespace OC.Workflow.Versioning.Implementation.Services
{
    public class WorkflowVersioningManager : IWorkflowVersioningManager
    {
        private readonly IDocumentManager<WorkflowVersioningDocument> _documentManager;
        private readonly IWorkflowVersionStore _versionStore;
        public WorkflowVersioningManager(IDocumentManager<WorkflowVersioningDocument> documentManager, IWorkflowVersionStore workflowVersionStore)
        {
            _documentManager = documentManager ?? throw new ArgumentNullException(nameof(documentManager));
            _versionStore = workflowVersionStore ?? throw new ArgumentNullException(nameof(workflowVersionStore));
        }

        public async Task<WorkflowVersionInfo> UpdateRestoreAsync(string workflowTypeId, long version)
        {
            WorkflowVersioningDocument document = await _documentManager.GetOrCreateMutableAsync();
            WorkflowVersionInfo? versionInfo = document.Information.GetValueOrDefault(workflowTypeId);
            if (versionInfo is null)
            {
                versionInfo = new();
            }
            versionInfo.LastRestoredTime = DateTime.UtcNow;
            versionInfo.PreviousVersion = versionInfo.CurrentVersion;
            versionInfo.CurrentVersion = version;
            document.Information[workflowTypeId] = versionInfo;
            await _documentManager.UpdateAsync(document);
            return versionInfo;
        }
        public async Task<WorkflowVersionInfo?> GetVersionInfoAsync(string workflowTypeId)
        {
            WorkflowVersioningDocument doc = await _documentManager.GetOrCreateImmutableAsync();

            return doc.Information.GetValueOrDefault(workflowTypeId);
        }
        public async Task<WorkflowVersionInfo> AddDefaultWorkflowVersionAsync(string workflowTypeId, long version)
        {
            WorkflowVersioningDocument document = await _documentManager.GetOrCreateMutableAsync();
            WorkflowVersionInfo? existing = document.Information.GetValueOrDefault(workflowTypeId);
            if (existing is not null)
            {
                return existing;
            }
            WorkflowVersionInfo versionInfo = new WorkflowVersionInfo
            {
                CurrentVersion = version,
                PreviousVersion = null,
                LastRestoredTime = null
            };
            document.Information[workflowTypeId] = versionInfo;
            await _documentManager.UpdateAsync(document);
            return versionInfo;
        }
        public async Task<WorkflowVersionInfo?> AddWorkflowVersionAsync(string workflowTypeId, string comment)
        {
            WorkflowVersioningDocument document = await _documentManager.GetOrCreateMutableAsync();

            WorkflowVersionInfo? existing = document.Information.GetValueOrDefault(workflowTypeId);

            long version = DateTime.UtcNow.Ticks;

            if (existing is null)
            {
                document.Information[workflowTypeId] = new WorkflowVersionInfo
                {
                    CurrentVersion = version,
                    PreviousVersion = null,
                    LastRestoredTime = null
                };
            }
            else
            {
                existing.PreviousVersion = existing.CurrentVersion;
                existing.CurrentVersion = version;
                document.Information[workflowTypeId] = existing;
            }
            await _versionStore.PersistAsync(workflowTypeId, version);
            await _documentManager.UpdateAsync(document);
            await _versionStore.PersistCommentAsync(workflowTypeId, version, comment);
            // write to file
            return document.Information.GetValueOrDefault(workflowTypeId);
        }
    }
}
