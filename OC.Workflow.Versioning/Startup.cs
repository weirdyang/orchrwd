using Microsoft.Extensions.DependencyInjection;
using OC.Workflow.Versioning.Implementation.Services;
using OrchardCore.Modules;
using OrchardCore.Navigation;

namespace OC.Workflow.Versioning;

[RequireFeatures("OrchardCore.Workflows")]
public sealed class Startup : StartupBase
{

    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IWorkflowVersioningManager, WorkflowVersioningManager>();
        services.AddScoped<IWorkflowVersionStore, WorkflowVersionStore>();
        services.AddNavigationProvider<WorkflowVersioningAdminMenu>();
    }
}

