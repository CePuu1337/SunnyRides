using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SunnyRides.Services.Database.Entities;

namespace SunnyRides.Services.Database.Configurations;

public class OpozvaniTokenConfiguration : IEntityTypeConfiguration<OpozvaniToken>
{
    public void Configure(EntityTypeBuilder<OpozvaniToken> builder)
    {
        builder.ToTable("OpozvaniToken");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Jti).IsRequired().HasMaxLength(100);

        // Middleware provjerava svaki zahtjev prema ovoj tabeli - indeks je obavezan
        builder.HasIndex(x => x.Jti).IsUnique();
        builder.HasIndex(x => x.DatumIsteka);
    }
}
