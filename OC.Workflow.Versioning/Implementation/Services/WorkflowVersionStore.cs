using Microsoft.Extensions.Options;
using OrchardCore.Json;
using OrchardCore.Workflows.Models;
using OrchardCore.Workflows.Services;
using System.Text.Json.Nodes;

namespace OC.Workflow.Versioning.Implementation.Services
{
    public class WorkflowVersionStore : IWorkflowVersionStore
    {
        private readonly IWorkflowTypeStore _store;
        private readonly IOptions<DocumentJsonSerializerOptions> _jsonSerializerOptions;

        public WorkflowVersionStore(IWorkflowTypeStore store, IOptions<DocumentJsonSerializerOptions> jsonSerializerOptions)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _jsonSerializerOptions = jsonSerializerOptions ?? throw new ArgumentNullException(nameof(jsonSerializerOptions));
        }

        public async Task<string> RetrieveActiveWorkflowTypeJsonAsync(string typeId)
        {
            WorkflowType? workflowType = await _store.GetAsync(typeId);
            if (workflowType is null)
            {
                throw new InvalidOperationException($"No workflow type with typeId: {typeId}");

            }
            JsonObject? active = JObject.FromObject(workflowType, _jsonSerializerOptions.Value.SerializerOptions);
            active?.Remove(nameof(workflowType.Id));
            return active?.ToString() ?? string.Empty;
        }

        public async Task<string> RetrieveVersionedWorkflowTypeJsonAsync(string typeId, long version)
        {
            string folderPath = WorkflowVersionSettings.VersionFolder(typeId);
            string fileName = WorkflowVersionSettings.VersionFileName(typeId, version);
            string filePath = Path.Combine(folderPath, fileName);

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"Version file does not exist: {filePath}");
            }

            return await File.ReadAllTextAsync(filePath);
        }

        public async Task<WorkflowType> RetrieveVersionedWorkflowTypeAsync(string typeId, long version)
        {
            string folderPath = WorkflowVersionSettings.VersionFolder(typeId);
            string fileName = WorkflowVersionSettings.VersionFileName(typeId, version);
            string filePath = Path.Combine(folderPath, fileName);

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"Version file does not exist: {filePath}");
            }

            string workflowJson = await File.ReadAllTextAsync(filePath);
            JsonObject? workflowJObject = JObject.Parse(workflowJson);
            if (workflowJObject is null)
            {
                throw new InvalidOperationException("Version file is not valid JSON.");
            }
            WorkflowType? workflowType;
            workflowJObject.Remove(nameof(workflowType.Id));
            workflowType = workflowJObject.ToObject<WorkflowType>(_jsonSerializerOptions.Value.SerializerOptions);

            return workflowType is null
                ? throw new InvalidOperationException("Unable to cast JSON to WorkflowType")
                : workflowType;
        }
        public async Task PersistCommentAsync(string typeId, long version, string comment)
        {
            if (string.IsNullOrWhiteSpace(comment))
            {
                return;
            }
            ;
            // Generate a file path using the workflow name/id
            string folderPath = WorkflowVersionSettings.VersionFolder(typeId);
            // Ensure directory exists
            Directory.CreateDirectory(folderPath);

            string fileName = WorkflowVersionSettings.VersionCommentFileName(typeId, version);
            string filePath = Path.Combine(folderPath, fileName);

            await File.WriteAllTextAsync(filePath, comment);
        }
        public async Task PersistAsync(string typeId, long version)
        {
            WorkflowType? workflowType = await _store.GetAsync(typeId);
            if (workflowType is null)
            {
                throw new InvalidOperationException($"No workflow type with id {typeId}");
            }

            string? workflowJson = JObject.FromObject(workflowType, _jsonSerializerOptions.Value.SerializerOptions)?.ToString();
            if (string.IsNullOrWhiteSpace(workflowJson))
            {
                throw new InvalidOperationException("Invalid workflow json");
            }

            // Generate a file path using the workflow name/id
            string folderPath = WorkflowVersionSettings.VersionFolder(typeId);
            // Ensure directory exists
            Directory.CreateDirectory(folderPath);

            string fileName = WorkflowVersionSettings.VersionFileName(typeId, version);
            string filePath = Path.Combine(folderPath, fileName);

            // Write the JSON to the file
            await File.WriteAllTextAsync(filePath, workflowJson);
        }
    }
}
