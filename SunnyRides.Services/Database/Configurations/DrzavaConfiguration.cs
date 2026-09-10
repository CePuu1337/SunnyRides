using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SunnyRides.Services.Database.Entities;

namespace SunnyRides.Services.Database.Configurations;

public class DrzavaConfiguration : IEntityTypeConfiguration<Drzava>
{
    public void Configure(EntityTypeBuilder<Drzava> builder)
    {
        builder.ToTable("Drzava");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Naziv).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Skracenica).IsRequired().HasMaxLength(10);
        builder.HasIndex(x => x.Naziv).IsUnique();
    }
}
