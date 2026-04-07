using EMenu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMenu.Infrastructure.Configurations
{
    public class IngredientProductConfiguration : IEntityTypeConfiguration<IngredientProduct>
    {
        public void Configure(EntityTypeBuilder<IngredientProduct> builder)
        {
            builder.ToTable("IngredientProducts");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Quantity)
                   .HasPrecision(10, 2);

            builder.HasOne(x => x.Product)
                   .WithMany(x => x.IngredientProducts)
                   .HasForeignKey(x => x.ProductID);

            builder.HasOne(x => x.Ingredient)
                   .WithMany(x => x.IngredientProducts)
                   .HasForeignKey(x => x.IngredientID);
        }
    }
}
