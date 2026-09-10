using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SunnyRides.Services.Database.Entities;

namespace SunnyRides.Services.Database.Configurations;

public class ObavijestConfiguration : IEntityTypeConfiguration<Obavijest>
{
    public void Configure(EntityTypeBuilder<Obavijest> builder)
    {
        builder.ToTable("Obavijest");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Naslov).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Tekst).IsRequired().HasMaxLength(2000);
        builder.Property(x => x.PutanjaSlike).HasMaxLength(500);

        builder.HasIndex(x => new { x.Aktivna, x.DatumObjave });
    }
}
