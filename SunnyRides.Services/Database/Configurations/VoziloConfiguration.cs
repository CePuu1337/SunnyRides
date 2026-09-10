using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SunnyRides.Services.Database.Entities;

namespace SunnyRides.Services.Database.Configurations;

public class VoziloConfiguration : IEntityTypeConfiguration<Vozilo>
{
    public void Configure(EntityTypeBuilder<Vozilo> builder)
    {
        builder.ToTable("Vozilo");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RegistarskaOznaka).IsRequired().HasMaxLength(20);
        builder.Property(x => x.DnevnaTarifa).HasPrecision(18, 2);
        builder.Property(x => x.SatnaTarifa).HasPrecision(18, 2);
        builder.Property(x => x.IznosDepozita).HasPrecision(18, 2);

        builder.HasOne(x => x.ModelVozila)
               .WithMany(m => m.Vozila)
               .HasForeignKey(x => x.ModelVozilaId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Poslovnica)
               .WithMany(p => p.Vozila)
               .HasForeignKey(x => x.PoslovnicaId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.RegistarskaOznaka).IsUnique();
    }
}
