namespace OC.Workflow.Versioning.Implementation.Models
{
    public class VersionDiffInfoModel
    {
        public VersionInfo VersionInfo { get; set; } = new();
        public VersionComment VersionComment { get; set; } = new();
    }
    public class DiffModel
    {
        public VersionDiffInfoModel Old { get; set; } = new();
        public VersionDiffInfoModel New { get; set; } = new();
    }
}
