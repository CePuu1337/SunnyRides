using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SunnyRides.Services.Database.Entities;

namespace SunnyRides.Services.Database.Configurations;

public class BlokadaVozilaConfiguration : IEntityTypeConfiguration<BlokadaVozila>
{
    public void Configure(EntityTypeBuilder<BlokadaVozila> builder)
    {
        builder.ToTable("BlokadaVozila");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Razlog).IsRequired().HasMaxLength(500);

        builder.HasOne(x => x.Vozilo)
               .WithMany(v => v.Blokade)
               .HasForeignKey(x => x.VoziloId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.KreiraoKorisnik)
               .WithMany()
               .HasForeignKey(x => x.KreiraoKorisnikId)
               .OnDelete(DeleteBehavior.Restrict);

        // Provjera dostupnosti filtrira po vozilu i periodu
        builder.HasIndex(x => new { x.VoziloId, x.DatumOd, x.DatumDo });
    }
}
