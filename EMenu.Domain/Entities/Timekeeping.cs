namespace EMenu.Domain.Entities
{
    public class Timekeeping
    {
        public int Id { get; set; }

        public int StaffID { get; set; }

        public DateOnly Date { get; set; }

        public DateTime CheckIn { get; set; }

        public DateTime? CheckOut { get; set; }

        public Staff Staff { get; set; }
    }
}
