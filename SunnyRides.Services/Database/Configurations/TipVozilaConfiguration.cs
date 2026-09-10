using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SunnyRides.Services.Database.Entities;

namespace SunnyRides.Services.Database.Configurations;

public class TipVozilaConfiguration : IEntityTypeConfiguration<TipVozila>
{
    public void Configure(EntityTypeBuilder<TipVozila> builder)
    {
        builder.ToTable("TipVozila");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Naziv).IsRequired().HasMaxLength(50);
        builder.HasIndex(x => x.Naziv).IsUnique();
    }
}
