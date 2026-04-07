using EMenu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMenu.Infrastructure.Configurations
{
    public class IngredientConfiguration : IEntityTypeConfiguration<Ingredient>
    {
        public void Configure(EntityTypeBuilder<Ingredient> builder)
        {
            builder.ToTable("Ingredients");

            builder.HasKey(x => x.IngredientID);

            builder.Property(x => x.Name)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(x => x.Unit)
                   .IsRequired()
                   .HasMaxLength(20);

            builder.Property(x => x.StockQuantity)
                   .HasPrecision(10, 2);

            builder.Property(x => x.MinStock)
                   .HasPrecision(10, 2);
        }
    }
}
