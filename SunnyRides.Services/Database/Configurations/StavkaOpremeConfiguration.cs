using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SunnyRides.Services.Database.Entities;

namespace SunnyRides.Services.Database.Configurations;

public class StavkaOpremeConfiguration : IEntityTypeConfiguration<StavkaOpreme>
{
    public void Configure(EntityTypeBuilder<StavkaOpreme> builder)
    {
        builder.ToTable("StavkaOpreme");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CijenaPoJedinici).HasPrecision(18, 2);
        builder.Property(x => x.Iznos).HasPrecision(18, 2);

        builder.HasOne(x => x.Rezervacija)
               .WithMany(r => r.StavkeOpreme)
               .HasForeignKey(x => x.RezervacijaId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.VrstaOpreme)
               .WithMany(v => v.Stavke)
               .HasForeignKey(x => x.VrstaOpremeId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.RezervacijaId, x.VrstaOpremeId }).IsUnique();
    }
}
