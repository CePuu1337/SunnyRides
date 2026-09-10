using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SunnyRides.Services.Database.Entities;

namespace SunnyRides.Services.Database.Configurations;

public class HistorijaPretrageConfiguration : IEntityTypeConfiguration<HistorijaPretrage>
{
    public void Configure(EntityTypeBuilder<HistorijaPretrage> builder)
    {
        builder.ToTable("HistorijaPretrage");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CijenaOd).HasPrecision(18, 2);
        builder.Property(x => x.CijenaDo).HasPrecision(18, 2);

        builder.HasOne(x => x.Korisnik)
               .WithMany(k => k.HistorijaPretraga)
               .HasForeignKey(x => x.KorisnikId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.TipVozila)
               .WithMany()
               .HasForeignKey(x => x.TipVozilaId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Grad)
               .WithMany()
               .HasForeignKey(x => x.GradId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Marka)
               .WithMany()
               .HasForeignKey(x => x.MarkaId)
               .OnDelete(DeleteBehavior.Restrict);

        // Recommender cita profil korisnika iz ovih zapisa
        builder.HasIndex(x => new { x.KorisnikId, x.DatumVrijeme });
    }
}
