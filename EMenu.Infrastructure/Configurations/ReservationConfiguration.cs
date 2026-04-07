using EMenu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMenu.Infrastructure.Configurations
{
    public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
    {
        public void Configure(EntityTypeBuilder<Reservation> builder)
        {
            builder.ToTable("Reservations");

            builder.HasKey(x => x.ReservationID);

            builder.HasOne(x => x.Customer)
                   .WithMany(x => x.Reservations)
                   .HasForeignKey(x => x.CustomerID);

            builder.HasOne(x => x.RestaurantTable)
                   .WithMany(x => x.Reservations)
                   .HasForeignKey(x => x.TableID);
        }
    }
}
