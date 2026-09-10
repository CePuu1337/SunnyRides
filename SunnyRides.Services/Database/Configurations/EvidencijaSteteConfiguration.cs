using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SunnyRides.Services.Database.Entities;

namespace SunnyRides.Services.Database.Configurations;

public class EvidencijaSteteConfiguration : IEntityTypeConfiguration<EvidencijaStete>
{
    public void Configure(EntityTypeBuilder<EvidencijaStete> builder)
    {
        builder.ToTable("EvidencijaStete");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Opis).IsRequired().HasMaxLength(1000);
        builder.Property(x => x.Iznos).HasPrecision(18, 2);

        // Najvise jedna evidencija stete po primopredaji
        builder.HasOne(x => x.Primopredaja)
               .WithOne(p => p.EvidencijaStete)
               .HasForeignKey<EvidencijaStete>(x => x.PrimopredajaId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.EvidentiraoKorisnik)
               .WithMany()
               .HasForeignKey(x => x.EvidentiraoKorisnikId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
