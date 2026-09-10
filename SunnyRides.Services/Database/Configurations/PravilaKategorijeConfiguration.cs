using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SunnyRides.Services.Database.Entities;

namespace SunnyRides.Services.Database.Configurations;

public class PravilaKategorijeConfiguration : IEntityTypeConfiguration<PravilaKategorije>
{
    public void Configure(EntityTypeBuilder<PravilaKategorije> builder)
    {
        builder.ToTable("PravilaKategorije");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.MaxSnagaKw).HasPrecision(6, 2);

        builder.HasOne(x => x.KategorijaDozvole)
               .WithMany(k => k.PravilaKategorija)
               .HasForeignKey(x => x.KategorijaDozvoleId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.TipVozila)
               .WithMany(t => t.PravilaKategorija)
               .HasForeignKey(x => x.TipVozilaId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.KategorijaDozvoleId, x.TipVozilaId }).IsUnique();
    }
}
