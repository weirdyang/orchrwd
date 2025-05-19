using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OC.Workflow.Versioning.Implementation.Models;
using OC.Workflow.Versioning.Implementation.Services;
using OrchardCore.Admin;
using OrchardCore.Json;
using OrchardCore.Modules;
using OrchardCore.Workflows.Models;
using OrchardCore.Workflows.Services;
using System.Text.Json.Nodes;

namespace OC.Workflow.Versioning.Controllers
{
    [Feature(ModuleConstants.ModuleName)]
    [Admin("Versioning/{action}/{id?}", "Versioning{action}")]
    public class VersioningController : Controller
    {
        private readonly IWorkflowTypeStore _workflowTypeStore;
        private readonly IWorkflowVersionStore _workflowVersionStore;
        private readonly IWorkflowVersioningManager _versioningManager;
        private readonly IOptions<DocumentJsonSerializerOptions> _jsonSerializerOptions;
        private readonly ILogger<VersioningController> _logger;

        public VersioningController(
            IWorkflowTypeStore workflowTypeStore,
            IWorkflowVersionStore workflowVersionStore,
            IWorkflowVersioningManager versioningManager,
            IOptions<DocumentJsonSerializerOptions> options,
            ILogger<VersioningController> logger)
        {
            _workflowTypeStore = workflowTypeStore ?? throw new ArgumentNullException(nameof(workflowTypeStore));
            _workflowVersionStore = workflowVersionStore ?? throw new ArgumentNullException(nameof(workflowVersionStore));
            _versioningManager = versioningManager ?? throw new ArgumentNullException(nameof(versioningManager));
            _jsonSerializerOptions = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;
        }

        public async Task<IActionResult> IndexAsync()
        {
            IEnumerable<WorkflowType> workflowTypes = await _workflowTypeStore.ListAsync();
            List<WorkflowVersionViewModel> vm = GetVersionViewModelsFromFileSystem(workflowTypes);
            return View("Index", vm);
        }
        private List<WorkflowVersionViewModel> GetVersionViewModelsFromFileSystem(IEnumerable<WorkflowType> workflowTypes)
        {
            List<WorkflowVersionViewModel> viewModels = new List<WorkflowVersionViewModel>();

            foreach (WorkflowType workflowType in workflowTypes)
            {
                string folderPath = WorkflowVersionSettings.VersionFolder(workflowType.WorkflowTypeId);
                int versionCount = 0;

                if (Directory.Exists(folderPath))
                {
                    string[] files = Directory.GetFiles(folderPath, $"{workflowType.WorkflowTypeId}.*.json");
                    versionCount = files.Length;
                }

                viewModels.Add(new WorkflowVersionViewModel
                {
                    WorkflowType = workflowType,
                    TotalVersions = versionCount
                });
            }

            return viewModels;
        }
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> CreatePostAsync([FromBody] CreateVersionRequest request)
        {
            if (string.IsNullOrEmpty(request.WorkflowTypeId))
            {
                return BadRequest("Workflow type ID is required");
            }

            WorkflowType workflowType = await _workflowTypeStore.GetAsync(request.WorkflowTypeId);
            if (workflowType == null)
            {
                return NotFound();
            }

            WorkflowVersionInfo? versionInfo = await _versioningManager.AddWorkflowVersionAsync(request.WorkflowTypeId, request.Comment ?? string.Empty);

            return Ok(versionInfo);
        }
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> RestorePostAsync([FromBody] VersionRestoreRequest request)
        {
            if (string.IsNullOrEmpty(request.WorkflowTypeId))
            {
                return BadRequest("Workflow type ID is required");
            }

            try
            {
                // Get the versioned workflow
                WorkflowType versionedWorkflow = await _workflowVersionStore.RetrieveVersionedWorkflowTypeAsync(
                    request.WorkflowTypeId,
                    request.Version);

                // Update the active workflow with the versioned one
                WorkflowType? existing = await _workflowTypeStore.GetAsync(request.WorkflowTypeId);
                if (existing is not null)
                {
                    await _workflowTypeStore.DeleteAsync(existing);
                }
                await _workflowTypeStore.SaveAsync(versionedWorkflow);
                // Update version info 
                WorkflowVersionInfo versionInfo = await _versioningManager.UpdateRestoreAsync(request.WorkflowTypeId, request.Version);

                return Ok(versionInfo);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogError(ex, "Error restoring workflow {@request}", request);
                return StatusCode(500, "An error occurred while restoring the version");
            }
        }
        public async Task<IActionResult> DetailsAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("Workflow type ID is required");
            }

            WorkflowType workflowType = await _workflowTypeStore.GetAsync(id);
            if (workflowType == null)
            {
                _logger.LogError("Could not find WorkflowType with WorkflowTypeId {id}", id);
                return NotFound();
            }

            WorkflowVersionInfo? versionInfo = await _versioningManager.GetVersionInfoAsync(id);
            if (versionInfo is null)
            {
                _logger.LogError("Could not find WorkflowVersionInfo with WorkflowTypeId {id}", id);
                return NotFound();
            }
            List<VersionInfo> versions = await GetVersionsFromFileSystemAsync(id);
            List<VersionComment> versionComments = await GetVersionCommentsFromFileSystemAsync(id);
            JsonObject? active = JObject.FromObject(workflowType, _jsonSerializerOptions.Value.SerializerOptions);
            active?.Remove(nameof(workflowType.Id));
            WorkflowVersionDetails model = new WorkflowVersionDetails
            {
                ActiveJson = active?.ToString() ?? string.Empty,
                WorkflowType = workflowType,
                VersionInfo = versionInfo,
                Versions = versions,
                VersionComments = versionComments
            };

            return View("Details", model);
        }
        private async Task<List<VersionComment>> GetVersionCommentsFromFileSystemAsync(string workflowTypeId)
        {
            List<VersionComment> result = new();
            string folderPath = WorkflowVersionSettings.VersionFolder(workflowTypeId);

            if (!Directory.Exists(folderPath))
            {
                return result;
            }
            IOrderedEnumerable<string> files = Directory
                .GetFiles(folderPath, $"{workflowTypeId}.*.comments.txt")
                .OrderByDescending(f => f);

            foreach (string? file in files)
            {
                string fileName = Path.GetFileName(file);
                // Extract version from filename (format: {typeId}.{version}.comments.txt)
                string[] parts = fileName.Split('.');
                if (parts.Length >= 3 && long.TryParse(parts[1], out long version))
                {
                    string comment = await System.IO.File.ReadAllTextAsync(file);
                    result.Add(new VersionComment
                    {
                        Comment = comment,
                        Version = version.ToString()
                    });
                }
            }

            return result;
        }
        private async Task<VersionComment> GetVersionCommentFromFileSystemAsync(string workflowTypeId, long version)
        {
            VersionComment result = new();
            result.Version = version.ToString();
            string folderPath = WorkflowVersionSettings.VersionFolder(workflowTypeId);

            if (!Directory.Exists(folderPath))
            {
                return result;
            }
            string? fileName = Directory
                .GetFiles(folderPath, $"{workflowTypeId}.{version}.comments.txt")
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(fileName))
            {
                string comment = await System.IO.File.ReadAllTextAsync(fileName);
                result.Comment = comment;
            }

            return result;
        }
        private async Task<List<VersionInfo>> GetVersionsFromFileSystemAsync(string workflowTypeId)
        {
            List<VersionInfo> result = new List<VersionInfo>();
            string folderPath = WorkflowVersionSettings.VersionFolder(workflowTypeId);

            if (!Directory.Exists(folderPath))
            {
                return result;
            }
            IOrderedEnumerable<string> files = Directory
                .GetFiles(folderPath, $"{workflowTypeId}.*.json")
                .OrderByDescending(f => f);

            foreach (string? file in files)
            {
                string fileName = Path.GetFileName(file);
                // Extract version from filename (format: {typeId}.{version}.json)
                string[] parts = fileName.Split('.');
                if (parts.Length >= 3 && long.TryParse(parts[1], out long version))
                {
                    string json = await System.IO.File.ReadAllTextAsync(file);
                    JsonObject? jObject = JObject.Parse(json);
                    jObject?.Remove(nameof(WorkflowType.Id));
                    result.Add(new VersionInfo
                    {
                        Version = version,
                        WorkflowTypeId = workflowTypeId,
                        Json = jObject?.ToString() ?? string.Empty
                    });
                }
            }

            return result;
        }
        public async Task<IActionResult> DiffAsync(string id, long? oldVersion, long? newVersion)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("Workflow type ID is required");
            }

            WorkflowType workflowType = await _workflowTypeStore.GetAsync(id);
            if (workflowType == null)
            {
                return NotFound();
            }

            // Get active workflow JSON
            string activeJson = await _workflowVersionStore.RetrieveActiveWorkflowTypeJsonAsync(id);

            // Get versions
            List<VersionInfo> versions = await GetVersionsFromFileSystemAsync(id);

            // Determine which versions to compare
            VersionInfo oldVersionInfo = new VersionInfo { WorkflowTypeId = id, Json = activeJson };
            VersionInfo newVersionInfo = new VersionInfo { WorkflowTypeId = id, Json = activeJson };
            VersionComment oldComment = new();
            VersionComment newComment = new();

            // If old version specified, find it
            if (oldVersion.HasValue)
            {
                VersionInfo? oldVersionData = versions.FirstOrDefault(v => v.Version == oldVersion.Value);
                if (oldVersionData != null)
                {
                    oldVersionInfo = oldVersionData;
                }
                oldComment = await GetVersionCommentFromFileSystemAsync(id, oldVersion.Value);
            }

            // If new version specified, find it
            if (newVersion.HasValue)
            {
                VersionInfo? newVersionData = versions.FirstOrDefault(v => v.Version == newVersion.Value);
                if (newVersionData != null)
                {
                    newVersionInfo = newVersionData;
                }
                newComment = await GetVersionCommentFromFileSystemAsync(id, newVersion.Value);
            }

            // Create the model
            DiffModel model = new DiffModel
            {
                Old = new()
                {
                    VersionInfo = oldVersionInfo,
                    VersionComment = oldComment
                },
                New = new()
                {
                    VersionInfo = newVersionInfo,
                    VersionComment = newComment
                }
            };

            return View("Diff", model);
        }
    }
}
