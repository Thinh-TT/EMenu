using EMenu.Application.Abstractions.DTOs;
using EMenu.Domain.Entities;

namespace EMenu.Web.ViewModels
{
    public class HrIndexViewModel
    {
        public int Year { get; set; }

        public int Month { get; set; }

        public bool IsAdmin { get; set; }

        public int? CurrentStaffId { get; set; }

        public int? SelectedStaffId { get; set; }

        public IReadOnlyList<Staff> Staffs { get; set; } = [];

        public IReadOnlyList<Timekeeping> Timekeepings { get; set; } = [];

        public MonthlyWageReportDto? StaffSummary { get; set; }

        public IReadOnlyList<MonthlyWageReportDto> MonthlySummary { get; set; } = [];
    }
}
