using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SunnyRides.Services.Database.Entities;

namespace SunnyRides.Services.Database.Configurations;

public class GradConfiguration : IEntityTypeConfiguration<Grad>
{
    public void Configure(EntityTypeBuilder<Grad> builder)
    {
        builder.ToTable("Grad");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Naziv).IsRequired().HasMaxLength(100);
        builder.Property(x => x.PostanskiBroj).HasMaxLength(20);

        builder.HasOne(x => x.Drzava)
               .WithMany(d => d.Gradovi)
               .HasForeignKey(x => x.DrzavaId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.DrzavaId, x.Naziv }).IsUnique();
    }
}
