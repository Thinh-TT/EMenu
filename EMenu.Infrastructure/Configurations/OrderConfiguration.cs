using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EMenu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMenu.Infrastructure.Configurations
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.ToTable("Order");

            builder.HasKey(x => x.OrderID);

            builder.Property(x => x.TotalAmount)
                   .HasPrecision(10, 2);

            builder.HasOne(x => x.OrderSession)
                   .WithMany(x => x.Orders)
                   .HasForeignKey(x => x.OrderSessionID);

            builder.HasOne(x => x.Staff)
                   .WithMany(x => x.Orders)
                   .HasForeignKey(x => x.StaffID);
        }
    }
}
