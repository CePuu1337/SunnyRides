using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SunnyRides.Services.Database.Entities;

namespace SunnyRides.Services.Database.Configurations;

public class PlacanjeConfiguration : IEntityTypeConfiguration<Placanje>
{
    public void Configure(EntityTypeBuilder<Placanje> builder)
    {
        builder.ToTable("Placanje");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Iznos).HasPrecision(18, 2);
        builder.Property(x => x.NaplaceniIznos).HasPrecision(18, 2);
        builder.Property(x => x.Valuta).IsRequired().HasMaxLength(10);
        builder.Property(x => x.Provider).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.ProviderPaymentIntentId).HasMaxLength(200);
        builder.Property(x => x.IdempotencyKey).HasMaxLength(200);

        builder.HasOne(x => x.Rezervacija)
               .WithMany(r => r.Placanja)
               .HasForeignKey(x => x.RezervacijaId)
               .OnDelete(DeleteBehavior.Restrict);

        // Jedno uspjesno placanje po rezervaciji (Succeeded = 3)
        builder.HasIndex(x => x.RezervacijaId)
               .IsUnique()
               .HasFilter("[Status] = 3");

        builder.HasIndex(x => x.ProviderPaymentIntentId);
    }
}
