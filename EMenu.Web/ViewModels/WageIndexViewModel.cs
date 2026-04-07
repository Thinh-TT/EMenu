using EMenu.Application.Abstractions.DTOs;
using EMenu.Domain.Entities;

namespace EMenu.Web.ViewModels
{
    public class WageIndexViewModel
    {
        public int Year { get; set; }

        public int Month { get; set; }

        public IReadOnlyList<Staff> Staffs { get; set; } = [];

        public IReadOnlyList<Wage> Wages { get; set; } = [];

        public IReadOnlyList<MonthlyWageReportDto> MonthlySummary { get; set; } = [];
    }
}
