using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SunnyRides.Services.Database.Entities;

namespace SunnyRides.Services.Database.Configurations;

public class RecenzijaConfiguration : IEntityTypeConfiguration<Recenzija>
{
    public void Configure(EntityTypeBuilder<Recenzija> builder)
    {
        builder.ToTable("Recenzija");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Komentar).HasMaxLength(1000);

        builder.HasOne(x => x.Korisnik)
               .WithMany(k => k.Recenzije)
               .HasForeignKey(x => x.KorisnikId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Vozilo)
               .WithMany(v => v.Recenzije)
               .HasForeignKey(x => x.VoziloId)
               .OnDelete(DeleteBehavior.Restrict);

        // Jedna recenzija po rezervaciji
        builder.HasOne(x => x.Rezervacija)
               .WithOne(r => r.Recenzija)
               .HasForeignKey<Recenzija>(x => x.RezervacijaId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.KorisnikId, x.RezervacijaId }).IsUnique();
        builder.HasIndex(x => new { x.VoziloId, x.Skrivena });
    }
}
