using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SunnyRides.Services.Database.Entities;

namespace SunnyRides.Services.Database.Configurations;

public class SlikaVozilaConfiguration : IEntityTypeConfiguration<SlikaVozila>
{
    public void Configure(EntityTypeBuilder<SlikaVozila> builder)
    {
        builder.ToTable("SlikaVozila");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Putanja).IsRequired().HasMaxLength(500);
        builder.Property(x => x.PutanjaThumbnail).IsRequired().HasMaxLength(500);

        // Slike se brisu zajedno sa vozilom
        builder.HasOne(x => x.Vozilo)
               .WithMany(v => v.Slike)
               .HasForeignKey(x => x.VoziloId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.VoziloId, x.Redoslijed });
    }
}
