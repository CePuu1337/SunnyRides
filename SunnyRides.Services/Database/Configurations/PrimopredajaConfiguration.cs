using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SunnyRides.Services.Database.Entities;

namespace SunnyRides.Services.Database.Configurations;

public class PrimopredajaConfiguration : IEntityTypeConfiguration<Primopredaja>
{
    public void Configure(EntityTypeBuilder<Primopredaja> builder)
    {
        builder.ToTable("Primopredaja");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Tip).HasConversion<int>();
        builder.Property(x => x.Napomena).HasMaxLength(1000);

        builder.HasOne(x => x.Rezervacija)
               .WithMany(r => r.Primopredaje)
               .HasForeignKey(x => x.RezervacijaId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.IzvrsioKorisnik)
               .WithMany()
               .HasForeignKey(x => x.IzvrsioKorisnikId)
               .OnDelete(DeleteBehavior.Restrict);

        // Jedna rezervacija ima najvise jedno izdavanje i jedan povrat
        builder.HasIndex(x => new { x.RezervacijaId, x.Tip }).IsUnique();
    }
}
