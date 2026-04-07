namespace EMenu.Application.Abstractions.DTOs
{
    public class MonthlyWageReportDto
    {
        public int StaffId { get; set; }

        public string StaffName { get; set; } = string.Empty;

        public int Year { get; set; }

        public int Month { get; set; }

        public int WorkDays { get; set; }

        public decimal TotalHours { get; set; }

        public decimal BaseSalary { get; set; }

        public decimal HourlyRate { get; set; }

        public decimal EstimatedWage { get; set; }
    }
}
