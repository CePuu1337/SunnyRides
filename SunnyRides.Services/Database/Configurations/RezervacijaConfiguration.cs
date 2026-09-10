using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SunnyRides.Services.Database.Entities;

namespace SunnyRides.Services.Database.Configurations;

public class RezervacijaConfiguration : IEntityTypeConfiguration<Rezervacija>
{
    public void Configure(EntityTypeBuilder<Rezervacija> builder)
    {
        builder.ToTable("Rezervacija");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Broj).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.UkupanIznos).HasPrecision(18, 2);
        builder.Property(x => x.IznosDepozita).HasPrecision(18, 2);
        builder.Property(x => x.IznosPopusta).HasPrecision(18, 2);
        builder.Property(x => x.RazlogOtkazivanja).HasMaxLength(500);

        builder.HasOne(x => x.Korisnik)
               .WithMany(k => k.Rezervacije)
               .HasForeignKey(x => x.KorisnikId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Vozilo)
               .WithMany(v => v.Rezervacije)
               .HasForeignKey(x => x.VoziloId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Poslovnica)
               .WithMany(p => p.Rezervacije)
               .HasForeignKey(x => x.PoslovnicaId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.PaketOsiguranja)
               .WithMany(p => p.Rezervacije)
               .HasForeignKey(x => x.PaketOsiguranjaId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.OtkazaoKorisnik)
               .WithMany()
               .HasForeignKey(x => x.OtkazaoKorisnikId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.Broj).IsUnique();

        // Zastita od dvostrukog slanja iste forme
        builder.HasIndex(x => new { x.KorisnikId, x.VoziloId, x.DatumOd }).IsUnique();

        // Provjera preklapanja termina ide kroz ovaj indeks
        builder.HasIndex(x => new { x.VoziloId, x.Status, x.DatumOd, x.DatumDo });
    }
}
