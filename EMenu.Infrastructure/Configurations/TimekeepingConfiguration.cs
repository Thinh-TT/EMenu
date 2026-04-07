using EMenu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMenu.Infrastructure.Configurations
{
    public class TimekeepingConfiguration : IEntityTypeConfiguration<Timekeeping>
    {
        public void Configure(EntityTypeBuilder<Timekeeping> builder)
        {
            builder.ToTable("Timekeeping");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Date)
                   .HasColumnType("date");

            builder.HasOne(x => x.Staff)
                   .WithMany(x => x.Timekeepings)
                   .HasForeignKey(x => x.StaffID);
        }
    }
}
