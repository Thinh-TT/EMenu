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
    public class ComboProductConfiguration : IEntityTypeConfiguration<ComboProduct>
    {
        public void Configure(EntityTypeBuilder<ComboProduct> builder)
        {
            builder.ToTable("Combo_Product");

            builder.HasKey(x => x.ComboProductID);

            builder.HasOne(x => x.Combo)
                   .WithMany(x => x.ComboProducts)
                   .HasForeignKey(x => x.ComboID)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Product)
                   .WithMany(x => x.ComboItems)
                   .HasForeignKey(x => x.ProductID)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
