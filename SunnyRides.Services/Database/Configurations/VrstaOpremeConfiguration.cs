using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SunnyRides.Services.Database.Entities;

namespace SunnyRides.Services.Database.Configurations;

public class VrstaOpremeConfiguration : IEntityTypeConfiguration<VrstaOpreme>
{
    public void Configure(EntityTypeBuilder<VrstaOpreme> builder)
    {
        builder.ToTable("VrstaOpreme");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Naziv).IsRequired().HasMaxLength(100);
        builder.Property(x => x.CijenaPoDanu).HasPrecision(18, 2);
        builder.Property(x => x.FiksnaCijena).HasPrecision(18, 2);
        builder.HasIndex(x => x.Naziv).IsUnique();
    }
}
