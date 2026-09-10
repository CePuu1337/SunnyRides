using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SunnyRides.Services.Database.Entities;

namespace SunnyRides.Services.Database.Configurations;

public class KodZaResetLozinkeConfiguration : IEntityTypeConfiguration<KodZaResetLozinke>
{
    public void Configure(EntityTypeBuilder<KodZaResetLozinke> builder)
    {
        builder.ToTable("KodZaResetLozinke");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.KodHash).IsRequired().HasMaxLength(200);

        builder.HasOne(x => x.Korisnik)
               .WithMany()
               .HasForeignKey(x => x.KorisnikId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.KorisnikId, x.Iskoristen });
    }
}
