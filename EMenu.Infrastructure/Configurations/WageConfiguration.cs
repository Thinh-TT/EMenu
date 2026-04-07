using EMenu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMenu.Infrastructure.Configurations
{
    public class WageConfiguration : IEntityTypeConfiguration<Wage>
    {
        public void Configure(EntityTypeBuilder<Wage> builder)
        {
            builder.ToTable("Wage");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.BaseSalary)
                   .HasPrecision(10, 2);

            builder.Property(x => x.HourlyRate)
                   .HasPrecision(10, 2);

            builder.HasIndex(x => x.StaffID)
                   .IsUnique();

            builder.HasOne(x => x.Staff)
                   .WithOne(x => x.Wage)
                   .HasForeignKey<Wage>(x => x.StaffID);
        }
    }
}
