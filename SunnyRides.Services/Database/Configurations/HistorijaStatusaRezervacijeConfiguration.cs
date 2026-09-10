using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SunnyRides.Services.Database.Entities;

namespace SunnyRides.Services.Database.Configurations;

public class HistorijaStatusaRezervacijeConfiguration : IEntityTypeConfiguration<HistorijaStatusaRezervacije>
{
    public void Configure(EntityTypeBuilder<HistorijaStatusaRezervacije> builder)
    {
        builder.ToTable("HistorijaStatusaRezervacije");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.StatusU).HasConversion<int>();
        builder.Property(x => x.Razlog).HasMaxLength(500);
        builder.Property(x => x.Opis).IsRequired().HasMaxLength(1000);

        builder.HasOne(x => x.Rezervacija)
               .WithMany(r => r.HistorijaStatusa)
               .HasForeignKey(x => x.RezervacijaId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.IzvrsioKorisnik)
               .WithMany()
               .HasForeignKey(x => x.IzvrsioKorisnikId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.RezervacijaId, x.DatumVrijeme });
    }
}
