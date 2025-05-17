using Microsoft.Extensions.Localization;
using OrchardCore.Navigation;
using OrchardCore.Workflows;

namespace OC.Workflow.Versioning.Implementation.Services
{
    class WorkflowVersioningAdminMenu : INavigationProvider
    {
        private readonly IStringLocalizer _s;
        public WorkflowVersioningAdminMenu(
            IStringLocalizer<WorkflowVersioningAdminMenu> localizer)
        {
            _s = localizer;
        }

        public ValueTask BuildNavigationAsync(string name, NavigationBuilder builder)
        {
            //InitializeSettings();
            if (!string.Equals(name, "admin", StringComparison.OrdinalIgnoreCase))
                return ValueTask.CompletedTask;

            builder
                .Add(_s["Workflow Versioning"], _s["Workflow Versioning"].PrefixPosition(), entry => entry
                .AddClass("workflow-versioning").Id("workflowVersioning")
                .Action("Index", "Versioning", ModuleConstants.ModuleName)
                .Permission(Permissions.ManageWorkflows)
                .LocalNav()
                );

            return ValueTask.CompletedTask;
        }
    }
}


