namespace EMenu.Domain.Entities
{
    public class Wage
    {
        public int Id { get; set; }

        public int StaffID { get; set; }

        public decimal BaseSalary { get; set; }

        public decimal HourlyRate { get; set; }

        public Staff Staff { get; set; }
    }
}
