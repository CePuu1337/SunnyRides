using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SunnyRides.Services.Database.Entities;

namespace SunnyRides.Services.Database.Configurations;

public class VozackaDozvolaConfiguration : IEntityTypeConfiguration<VozackaDozvola>
{
    public void Configure(EntityTypeBuilder<VozackaDozvola> builder)
    {
        builder.ToTable("VozackaDozvola");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.BrojDozvole).IsRequired().HasMaxLength(50);
        builder.Property(x => x.PutanjaSlike).HasMaxLength(500);
        builder.Property(x => x.RazlogOdbijanja).HasMaxLength(500);
        builder.Property(x => x.Status).HasConversion<int>();

        // Jedan korisnik ima najvise jednu dozvolu
        builder.HasOne(x => x.Korisnik)
               .WithOne(k => k.VozackaDozvola)
               .HasForeignKey<VozackaDozvola>(x => x.KorisnikId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.VerifikovaoKorisnik)
               .WithMany()
               .HasForeignKey(x => x.VerifikovaoKorisnikId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.BrojDozvole).IsUnique();
    }
}
