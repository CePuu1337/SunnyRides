using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SunnyRides.Services.Database.Entities;

namespace SunnyRides.Services.Database.Configurations;

public class DozvolaKategorijaConfiguration : IEntityTypeConfiguration<DozvolaKategorija>
{
    public void Configure(EntityTypeBuilder<DozvolaKategorija> builder)
    {
        builder.ToTable("DozvolaKategorija");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.VozackaDozvola)
               .WithMany(d => d.Kategorije)
               .HasForeignKey(x => x.VozackaDozvolaId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.KategorijaDozvole)
               .WithMany(k => k.DozvoleKategorije)
               .HasForeignKey(x => x.KategorijaDozvoleId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.VozackaDozvolaId, x.KategorijaDozvoleId }).IsUnique();
    }
}
