using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SunnyRides.Services.Database.Entities;

namespace SunnyRides.Services.Database.Configurations;

public class KorisnikRoleConfiguration : IEntityTypeConfiguration<KorisnikRole>
{
    public void Configure(EntityTypeBuilder<KorisnikRole> builder)
    {
        builder.ToTable("KorisnikRole");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Korisnik)
               .WithMany(k => k.KorisnikRole)
               .HasForeignKey(x => x.KorisnikId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Role)
               .WithMany(r => r.KorisnikRole)
               .HasForeignKey(x => x.RoleId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.KorisnikId, x.RoleId }).IsUnique();
    }
}
