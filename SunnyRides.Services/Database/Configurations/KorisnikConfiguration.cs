using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SunnyRides.Services.Database.Entities;

namespace SunnyRides.Services.Database.Configurations;

public class KorisnikConfiguration : IEntityTypeConfiguration<Korisnik>
{
    public void Configure(EntityTypeBuilder<Korisnik> builder)
    {
        builder.ToTable("Korisnik");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.KorisnickoIme).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Ime).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Prezime).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Email).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Telefon).HasMaxLength(30);
        builder.Property(x => x.LozinkaHash).IsRequired().HasMaxLength(200);
        builder.Property(x => x.PutanjaSlike).HasMaxLength(500);

        // Prijava ide preko korisnickog imena, email je jedinstven radi reseta lozinke
        builder.HasIndex(x => x.KorisnickoIme).IsUnique();
        builder.HasIndex(x => x.Email).IsUnique();
    }
}
