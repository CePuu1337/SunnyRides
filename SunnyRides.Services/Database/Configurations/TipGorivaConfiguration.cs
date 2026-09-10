using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SunnyRides.Services.Database.Entities;

namespace SunnyRides.Services.Database.Configurations;

public class TipGorivaConfiguration : IEntityTypeConfiguration<TipGoriva>
{
    public void Configure(EntityTypeBuilder<TipGoriva> builder)
    {
        builder.ToTable("TipGoriva");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Naziv).IsRequired().HasMaxLength(50);
        builder.HasIndex(x => x.Naziv).IsUnique();
    }
}
