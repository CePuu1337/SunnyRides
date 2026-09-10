using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SunnyRides.Services.Database.Entities;

namespace SunnyRides.Services.Database.Configurations;

public class KategorijaDozvoleConfiguration : IEntityTypeConfiguration<KategorijaDozvole>
{
    public void Configure(EntityTypeBuilder<KategorijaDozvole> builder)
    {
        builder.ToTable("KategorijaDozvole");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Oznaka).IsRequired().HasMaxLength(10);
        builder.Property(x => x.Opis).HasMaxLength(200);
        builder.HasIndex(x => x.Oznaka).IsUnique();
    }
}
