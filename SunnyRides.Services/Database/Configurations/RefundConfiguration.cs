using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SunnyRides.Services.Database.Entities;

namespace SunnyRides.Services.Database.Configurations;

public class RefundConfiguration : IEntityTypeConfiguration<Refund>
{
    public void Configure(EntityTypeBuilder<Refund> builder)
    {
        builder.ToTable("Refund");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Iznos).HasPrecision(18, 2);
        builder.Property(x => x.Razlog).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.ProviderRefundId).HasMaxLength(200);

        builder.HasOne(x => x.Placanje)
               .WithMany(p => p.Refundi)
               .HasForeignKey(x => x.PlacanjeId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.KreiraoKorisnik)
               .WithMany()
               .HasForeignKey(x => x.KreiraoKorisnikId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
