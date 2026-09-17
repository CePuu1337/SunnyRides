using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SunnyRides.Services.Database.Entities;

namespace SunnyRides.Services.Database.Configurations;

public class RazlogOtkazivanjaConfiguration : IEntityTypeConfiguration<RazlogOtkazivanja>
{
    public void Configure(EntityTypeBuilder<RazlogOtkazivanja> builder)
    {
        builder.ToTable("RazlogOtkazivanja");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Naziv).IsRequired().HasMaxLength(100);
        builder.HasIndex(x => x.Naziv).IsUnique();
    }
}
