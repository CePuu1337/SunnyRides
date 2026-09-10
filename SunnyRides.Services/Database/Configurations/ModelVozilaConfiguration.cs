using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SunnyRides.Services.Database.Entities;

namespace SunnyRides.Services.Database.Configurations;

public class ModelVozilaConfiguration : IEntityTypeConfiguration<ModelVozila>
{
    public void Configure(EntityTypeBuilder<ModelVozila> builder)
    {
        builder.ToTable("ModelVozila");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Naziv).IsRequired().HasMaxLength(100);
        builder.Property(x => x.SnagaKw).HasPrecision(6, 2);

        builder.HasOne(x => x.Marka)
               .WithMany(m => m.Modeli)
               .HasForeignKey(x => x.MarkaId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.TipVozila)
               .WithMany(t => t.Modeli)
               .HasForeignKey(x => x.TipVozilaId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.TipGoriva)
               .WithMany(t => t.Modeli)
               .HasForeignKey(x => x.TipGorivaId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.KategorijaDozvole)
               .WithMany(k => k.Modeli)
               .HasForeignKey(x => x.KategorijaDozvoleId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.MarkaId, x.Naziv }).IsUnique();
    }
}
