using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OC.Workflow.Versioning.Implementation.Services;
using OrchardCore.Modules;
using OrchardCore.Navigation;
using OrchardCore.Security.Permissions;
using OrchardCore.Settings;
using OrchardCore.Workflows.Models;
using OrchardCore.Workflows.Services;
using YesSql;

namespace OC.Workflow.Versioning;
public class AddAuthorizeFiltersControllerConvention : IControllerModelConvention
{
    public void Apply(ControllerModel controller)
    {
        if (controller.ControllerName.ToUpper().Contains("WORKFLOW"))
        {
            controller.Filters.Add(new AuthorizeFilter("WorkflowRestriction"));
        }
    }
}
public class WorkflowRestrictionPermissions : IPermissionProvider
{
    public static readonly Permission CreateWorkflow = new Permission(nameof(CreateWorkflow), "Allow creation of workflows", isSecurityCritical: true);
    public static readonly Permission EditWorkflow = new Permission(nameof(EditWorkflow), "Allow editing of Workflows", isSecurityCritical: true);
    public Task<IEnumerable<Permission>> GetPermissionsAsync()
    {
        return Task.FromResult(GetPermissions());
    }
    private IEnumerable<Permission> GetPermissions()
    {
        return new[] {
                        CreateWorkflow,
                        EditWorkflow,
                        OrchardCore.Workflows.Permissions.ManageWorkflows,
                        OrchardCore.Admin.AdminPermissions.AccessAdminPanel,
                        OrchardCore.Workflows.Permissions.ExecuteWorkflows
           };
    }
    public IEnumerable<PermissionStereotype> GetDefaultStereotypes()
    {
        return new[] {
                new PermissionStereotype {
                    Name = "TrialUser",
                    Permissions = GetPermissions()
                }
            };
    }
}
/// <summary>
/// https://docs.microsoft.com/en-us/aspnet/core/security/authorization/policies?view=aspnetcore-3.1
/// marker class
/// no properties
/// </summary>
public class WorkflowRestrictionRequirement : IAuthorizationRequirement
{
    public static string Name => nameof(WorkflowRestrictionRequirement);

    public static int DefaultMax => 10;
    public int MaximumWorkflows { get; }

    public WorkflowRestrictionRequirement(int maxNumber)
    {
        MaximumWorkflows = maxNumber;
    }
}
public class WorkflowRestrictionSettings
{
    public int MaximumWorkflows { get; set; } = WorkflowRestrictionRequirement.DefaultMax;
    public List<string> AllowedUsers { get; set; } = new List<string> { "admin" };
    public List<string> LockedWorkflows { get; set; } = new List<string> { "4hcfxp9b626z66585r9q3t92cs" };
}
public class WorkflowRestrictionSettingsConfiguration : IConfigureOptions<WorkflowRestrictionSettings>
{
    private readonly ISiteService _siteService;
    public WorkflowRestrictionSettingsConfiguration(ISiteService siteService) => _siteService = siteService;
    public void Configure(WorkflowRestrictionSettings options)
    {
        if (options == null)
        {
            options = new WorkflowRestrictionSettings();

        }
        ;
        WorkflowRestrictionSettings settings = _siteService.GetSiteSettingsAsync()
            .GetAwaiter().GetResult()
            .As<WorkflowRestrictionSettings>();
        if (settings is not null)
        {
            options.MaximumWorkflows = settings.MaximumWorkflows;
            options.LockedWorkflows = settings.LockedWorkflows;
            options.AllowedUsers = settings.AllowedUsers;
        }

    }
}
public class WorkflowRestrictionHandler : AuthorizationHandler<WorkflowRestrictionRequirement>
{
    private readonly ILogger<WorkflowRestrictionHandler> _logger;
    private readonly IWorkflowTypeStore _workflowStore;
    private readonly WorkflowRestrictionSettings _restrictionSettings;
    private readonly ISession _session;
    public WorkflowRestrictionHandler(
        ILogger<WorkflowRestrictionHandler> logger,
        IWorkflowTypeStore workflowStore,
        IOptions<WorkflowRestrictionSettings> options,
        ISession session)
    {

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _workflowStore = workflowStore;
        _restrictionSettings = options.Value;
        _session = session;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        WorkflowRestrictionRequirement requirement)
    {
        if (!MustCheck(context, WorkflowRestrictionRequirement.Name))
        {
            context.Succeed(requirement);
        }
        ;
        if (context.Resource is Microsoft.AspNetCore.Mvc.Filters.AuthorizationFilterContext resource)
        {
            ActionDescriptor actionDescriptor = resource.ActionDescriptor;
            //[HttpGet]
            //public async Task<IActionResult> EditProperties(WorkflowTypePropertiesViewModel viewModel, int? id)
            //if id == null ==> user is trying to create a new workflow
            _logger.LogInformation(actionDescriptor.DisplayName);
            if (actionDescriptor.RouteValues["action"] == "EditProperties"
               && !resource.RouteData.Values.ContainsKey("ID")
               && resource.HttpContext.Request.Method == "GET")
            {
                //bool canCreate = await _authService.AuthorizeAsync(context.User, WorkflowRestrictionPermissions.CreateWorkflow);
                //if (!canCreate)
                //{
                //    await _notifier.ErrorAsync(_h["You do not have the permission to create workflows"]);
                //    context.Fail();
                //    return;
                //}
                context.Succeed(requirement);
                return;
            }
            else if (actionDescriptor.RouteValues["action"] == "Edit"
               && resource.RouteData.Values.ContainsKey("ID")
               && resource.HttpContext.Request.Method == "GET")
            {
                bool test = resource.RouteData.Values.TryGetValue("ID", out object? asd);
                string what = asd.GetType().Name;
                if (resource.RouteData.Values.TryGetValue("ID", out object? workflowTypeId) && long.TryParse(workflowTypeId?.ToString(), out long id))
                {
                    WorkflowType workflowType = await _session.GetAsync<WorkflowType>(id);
                    if (workflowType is null || !_restrictionSettings.LockedWorkflows.Contains(workflowType.WorkflowTypeId))
                    {
                        context.Succeed(requirement);
                        return;
                    }

                    if (!_restrictionSettings.AllowedUsers.Contains(context.User.Identity?.Name ?? string.Empty))
                    {
                        //await resource.HttpContext
                        //    .RequestServices
                        //    .GetRequiredService<INotifier>()
                        //    .ErrorAsync(new LocalizedHtmlString("message", "You do not not have permission to view/edit this Workflow as it has been locked"));
                        //context.Succeed(requirement);
                        //Microsoft.AspNetCore.Http.PathString pathbase = resource.HttpContext.Request.PathBase;
                        //resource.HttpContext.Response.Redirect($"{pathbase}/admin");
                        //return;
                        context.Fail();
                        return;
                    }
                    context.Succeed(requirement);
                    return;
                }
            }
            context.Succeed(requirement);
            return;
        }
    }
    protected bool MustCheck(AuthorizationHandlerContext context, string permissionName)
    {
        return context.User.Identity is not null && context.User.Identity.IsAuthenticated;
    }
}
[RequireFeatures("OrchardCore.Workflows")]
public sealed class Startup : StartupBase
{

    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("WorkflowRestriction", policy =>
            {
                policy.Requirements.Add(new WorkflowRestrictionRequirement(WorkflowRestrictionRequirement.DefaultMax));
            });
        });
        services.AddMvc(o =>
        {
            o.Conventions.Add(new AddAuthorizeFiltersControllerConvention());
        });
        services.AddScoped<IAuthorizationHandler, WorkflowRestrictionHandler>();

        services.Configure<WorkflowRestrictionSettings>(opt =>
        {
            opt.MaximumWorkflows = WorkflowRestrictionRequirement.DefaultMax;
            opt.AllowedUsers = new List<string> { "admin" };
            opt.LockedWorkflows = new List<string> { "4hcfxp9b626z66585r9q3t92cs" };
        });
        services.AddTransient<IConfigureOptions<WorkflowRestrictionSettings>, WorkflowRestrictionSettingsConfiguration>();
        services.AddScoped<IWorkflowVersioningManager, WorkflowVersioningManager>();
        services.AddScoped<IWorkflowVersionStore, WorkflowVersionStore>();
        services.AddNavigationProvider<WorkflowVersioningAdminMenu>();
        services.AddPermissionProvider<WorkflowRestrictionPermissions>();
    }
}

