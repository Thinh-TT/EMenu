namespace EMenu.Application.Abstractions.DTOs
{
    public class SessionTableOperationResultDto
    {
        public string Operation { get; set; } = string.Empty;

        public int SourceTableId { get; set; }

        public int TargetTableId { get; set; }

        public int SourceSessionId { get; set; }

        public int TargetSessionId { get; set; }

        public int MovedOrderCount { get; set; }
    }
}
