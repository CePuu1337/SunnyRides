using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SunnyRides.Services.Database.Entities;

namespace SunnyRides.Services.Database.Configurations;

public class PoslovnicaConfiguration : IEntityTypeConfiguration<Poslovnica>
{
    public void Configure(EntityTypeBuilder<Poslovnica> builder)
    {
        builder.ToTable("Poslovnica");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Naziv).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Adresa).IsRequired().HasMaxLength(200);
        builder.Property(x => x.RadnoVrijeme).HasMaxLength(100);

        builder.HasOne(x => x.Grad)
               .WithMany(g => g.Poslovnice)
               .HasForeignKey(x => x.GradId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.Naziv).IsUnique();
    }
}
