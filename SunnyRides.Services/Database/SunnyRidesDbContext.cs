using Microsoft.EntityFrameworkCore;
using SunnyRides.Services.Database.Entities;

namespace SunnyRides.Services.Database;

public class SunnyRidesDbContext : DbContext
{
    public SunnyRidesDbContext(DbContextOptions<SunnyRidesDbContext> options)
        : base(options)
    {
    }

    // Grupa 1 - sifrarnici
    public DbSet<Drzava> Drzave => Set<Drzava>();
    public DbSet<Grad> Gradovi => Set<Grad>();
    public DbSet<Poslovnica> Poslovnice => Set<Poslovnica>();
    public DbSet<TipVozila> TipoviVozila => Set<TipVozila>();
    public DbSet<Marka> Marke => Set<Marka>();
    public DbSet<ModelVozila> ModeliVozila => Set<ModelVozila>();
    public DbSet<TipGoriva> TipoviGoriva => Set<TipGoriva>();
    public DbSet<KategorijaDozvole> KategorijeDozvola => Set<KategorijaDozvole>();
    public DbSet<PravilaKategorije> PravilaKategorija => Set<PravilaKategorije>();
    public DbSet<VrstaOpreme> VrsteOpreme => Set<VrstaOpreme>();
    public DbSet<StanjeOpreme> StanjaOpreme => Set<StanjeOpreme>();
    public DbSet<PaketOsiguranja> PaketiOsiguranja => Set<PaketOsiguranja>();
    public DbSet<Role> Role => Set<Role>();

    // Korisnici i pristup
    public DbSet<Korisnik> Korisnici => Set<Korisnik>();
    public DbSet<KorisnikRole> KorisnikRole => Set<KorisnikRole>();
    public DbSet<VozackaDozvola> VozackeDozvole => Set<VozackaDozvola>();
    public DbSet<DozvolaKategorija> DozvolaKategorije => Set<DozvolaKategorija>();
    public DbSet<OpozvaniToken> OpozvaniTokeni => Set<OpozvaniToken>();
    public DbSet<KodZaResetLozinke> KodoviZaResetLozinke => Set<KodZaResetLozinke>();

    // Flota
    public DbSet<Vozilo> Vozila => Set<Vozilo>();
    public DbSet<SlikaVozila> SlikeVozila => Set<SlikaVozila>();
    public DbSet<BlokadaVozila> BlokadeVozila => Set<BlokadaVozila>();
    public DbSet<Cjenovnik> Cjenovnici => Set<Cjenovnik>();

    // Poslovanje
    public DbSet<Rezervacija> Rezervacije => Set<Rezervacija>();
    public DbSet<StavkaOpreme> StavkeOpreme => Set<StavkaOpreme>();
    public DbSet<Placanje> Placanja => Set<Placanje>();
    public DbSet<Refund> Refundi => Set<Refund>();
    public DbSet<ObradjeniWebhookEvent> ObradjeniWebhookEventi => Set<ObradjeniWebhookEvent>();
    public DbSet<Primopredaja> Primopredaje => Set<Primopredaja>();
    public DbSet<FotografijaPrimopredaje> FotografijePrimopredaje => Set<FotografijaPrimopredaje>();
    public DbSet<EvidencijaStete> EvidencijeStete => Set<EvidencijaStete>();
    public DbSet<HistorijaStatusaRezervacije> HistorijaStatusaRezervacija => Set<HistorijaStatusaRezervacije>();

    // Ostalo
    public DbSet<Recenzija> Recenzije => Set<Recenzija>();
    public DbSet<Notifikacija> Notifikacije => Set<Notifikacija>();
    public DbSet<Obavijest> Obavijesti => Set<Obavijest>();
    public DbSet<HistorijaPretrage> HistorijaPretraga => Set<HistorijaPretrage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Sve konfiguracije zive u zasebnim klasama u Database/Configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SunnyRidesDbContext).Assembly);
    }
}
