namespace EMenu.Web.ViewModels
{
    public class SessionTableActionRequest
    {
        public int SourceTableId { get; set; }

        public int TargetTableId { get; set; }

        public string? Actor { get; set; }
    }
}
