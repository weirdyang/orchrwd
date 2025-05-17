namespace OC.Workflow.Versioning.Implementation.Models
{
    public class DiffModel
    {
        public VersionInfo Old { get; set; } = new();
        public VersionInfo New { get; set; } = new();
    }
}
