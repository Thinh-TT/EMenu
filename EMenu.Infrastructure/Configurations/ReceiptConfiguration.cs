using EMenu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMenu.Infrastructure.Configurations
{
    public class ReceiptConfiguration : IEntityTypeConfiguration<Receipt>
    {
        public void Configure(EntityTypeBuilder<Receipt> builder)
        {
            builder.ToTable("Receipts");

            builder.HasKey(x => x.ReceiptID);

            builder.HasOne(x => x.Supplier)
                   .WithMany(x => x.Receipts)
                   .HasForeignKey(x => x.SupplierID);

            builder.HasOne(x => x.Staff)
                   .WithMany(x => x.Receipts)
                   .HasForeignKey(x => x.StaffID);
        }
    }
}
