using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "OC.Workflow.Versioning",
    Author = "The Orchard Core Team",
    Website = "https://orchardcore.net",
    Version = "0.0.1",
    Description = "OC.Workflow.Versioning enables versioning of workflows.",
    Category = "Workflows"
)]
[assembly: Feature(
    Id = ModuleConstants.ModuleName,
    Name = ModuleConstants.ModuleName,
    Description = "OC.Workflow.Versioning enables versioning of workflows.",
    Dependencies = new[] { "OrchardCore.Workflows", "OrchardCore.Resources" },
    Category = "Workflows"
)]
