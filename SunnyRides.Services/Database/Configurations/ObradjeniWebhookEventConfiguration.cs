using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SunnyRides.Services.Database.Entities;

namespace SunnyRides.Services.Database.Configurations;

public class ObradjeniWebhookEventConfiguration : IEntityTypeConfiguration<ObradjeniWebhookEvent>
{
    public void Configure(EntityTypeBuilder<ObradjeniWebhookEvent> builder)
    {
        builder.ToTable("ObradjeniWebhookEvent");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProviderEventId).IsRequired().HasMaxLength(200);
        builder.Property(x => x.TipEventa).IsRequired().HasMaxLength(100);

        // Postojanje zapisa znaci da je dogadjaj vec obradjen
        builder.HasIndex(x => x.ProviderEventId).IsUnique();
    }
}
