using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SunnyRides.Services.Database.Entities;

namespace SunnyRides.Services.Database.Configurations;

public class FotografijaPrimopredajeConfiguration : IEntityTypeConfiguration<FotografijaPrimopredaje>
{
    public void Configure(EntityTypeBuilder<FotografijaPrimopredaje> builder)
    {
        builder.ToTable("FotografijaPrimopredaje");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Putanja).IsRequired().HasMaxLength(500);
        builder.Property(x => x.PutanjaThumbnail).IsRequired().HasMaxLength(500);

        builder.HasOne(x => x.Primopredaja)
               .WithMany(p => p.Fotografije)
               .HasForeignKey(x => x.PrimopredajaId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
