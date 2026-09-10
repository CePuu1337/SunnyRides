using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SunnyRides.Services.Database.Entities;

namespace SunnyRides.Services.Database.Configurations;

public class MarkaConfiguration : IEntityTypeConfiguration<Marka>
{
    public void Configure(EntityTypeBuilder<Marka> builder)
    {
        builder.ToTable("Marka");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Naziv).IsRequired().HasMaxLength(100);
        builder.HasIndex(x => x.Naziv).IsUnique();
    }
}
