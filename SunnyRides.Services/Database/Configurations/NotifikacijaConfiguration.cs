using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SunnyRides.Services.Database.Entities;

namespace SunnyRides.Services.Database.Configurations;

public class NotifikacijaConfiguration : IEntityTypeConfiguration<Notifikacija>
{
    public void Configure(EntityTypeBuilder<Notifikacija> builder)
    {
        builder.ToTable("Notifikacija");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Naslov).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Tekst).IsRequired().HasMaxLength(1000);
        builder.Property(x => x.Tip).HasConversion<int>();

        builder.HasOne(x => x.Korisnik)
               .WithMany(k => k.Notifikacije)
               .HasForeignKey(x => x.KorisnikId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Rezervacija)
               .WithMany()
               .HasForeignKey(x => x.RezervacijaId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.KorisnikId, x.Procitana, x.DatumKreiranja });
    }
}
