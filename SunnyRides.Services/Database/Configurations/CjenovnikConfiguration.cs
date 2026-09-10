using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SunnyRides.Services.Database.Entities;

namespace SunnyRides.Services.Database.Configurations;

public class CjenovnikConfiguration : IEntityTypeConfiguration<Cjenovnik>
{
    public void Configure(EntityTypeBuilder<Cjenovnik> builder)
    {
        builder.ToTable("Cjenovnik");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Naziv).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Mnozilac).HasPrecision(5, 2);
        builder.Property(x => x.SatnaTarifa).HasPrecision(18, 2);
        builder.Property(x => x.DnevnaTarifa).HasPrecision(18, 2);
        builder.Property(x => x.PopustProcenat1).HasPrecision(5, 2);
        builder.Property(x => x.PopustProcenat2).HasPrecision(5, 2);

        builder.HasOne(x => x.ModelVozila)
               .WithMany(m => m.Cjenovnici)
               .HasForeignKey(x => x.ModelVozilaId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ModelVozilaId, x.DatumOd, x.DatumDo });
    }
}
