using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SunnyRides.Services.Database.Entities;

namespace SunnyRides.Services.Database.Configurations;

public class PaketOsiguranjaConfiguration : IEntityTypeConfiguration<PaketOsiguranja>
{
    public void Configure(EntityTypeBuilder<PaketOsiguranja> builder)
    {
        builder.ToTable("PaketOsiguranja");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Naziv).IsRequired().HasMaxLength(100);
        builder.Property(x => x.CijenaPoDanu).HasPrecision(18, 2);
        builder.Property(x => x.IznosUcesca).HasPrecision(18, 2);
        builder.HasIndex(x => x.Naziv).IsUnique();
    }
}
