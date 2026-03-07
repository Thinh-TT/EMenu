using EMenu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMenu.Infrastructure.Configurations
{
    public class OrderProductConfiguration : IEntityTypeConfiguration<OrderProduct>
    {
        public void Configure(EntityTypeBuilder<OrderProduct> builder)
        {
            builder.ToTable("Order_Product");

            builder.HasKey(x => x.OrderProductID);

            builder.Property(x => x.Price)
                   .HasPrecision(10, 2);

            builder.HasOne(x => x.Order)
                   .WithMany(x => x.OrderProducts)
                   .HasForeignKey(x => x.OrderID);

            builder.HasOne(x => x.Product)
                   .WithMany()
                   .HasForeignKey(x => x.ProductID);
        }
    }
}
