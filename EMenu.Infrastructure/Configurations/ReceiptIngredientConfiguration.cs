using EMenu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMenu.Infrastructure.Configurations
{
    public class ReceiptIngredientConfiguration : IEntityTypeConfiguration<ReceiptIngredient>
    {
        public void Configure(EntityTypeBuilder<ReceiptIngredient> builder)
        {
            builder.ToTable("ReceiptIngredients");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Quantity)
                   .HasPrecision(10, 2);

            builder.Property(x => x.Price)
                   .HasPrecision(10, 2);

            builder.HasOne(x => x.Receipt)
                   .WithMany(x => x.ReceiptIngredients)
                   .HasForeignKey(x => x.ReceiptID);

            builder.HasOne(x => x.Ingredient)
                   .WithMany(x => x.ReceiptIngredients)
                   .HasForeignKey(x => x.IngredientID);
        }
    }
}
