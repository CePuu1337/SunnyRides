using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SunnyRides.Services.Database.Entities;

namespace SunnyRides.Services.Database.Configurations;

public class StanjeOpremeConfiguration : IEntityTypeConfiguration<StanjeOpreme>
{
    public void Configure(EntityTypeBuilder<StanjeOpreme> builder)
    {
        builder.ToTable("StanjeOpreme");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.VrstaOpreme)
               .WithMany(v => v.Stanja)
               .HasForeignKey(x => x.VrstaOpremeId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Poslovnica)
               .WithMany(p => p.StanjaOpreme)
               .HasForeignKey(x => x.PoslovnicaId)
               .OnDelete(DeleteBehavior.Restrict);

        // Jedno stanje po kombinaciji opreme i poslovnice
        builder.HasIndex(x => new { x.VrstaOpremeId, x.PoslovnicaId }).IsUnique();
    }
}
