using Mapster;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Enums;
using SunnyRides.Services.Database.Entities;

namespace SunnyRides.Services.Mapping;

/// <summary>
/// Mapster radi po konvenciji - svojstva istog naziva se poklapaju sama. Ovdje se
/// registruju samo izuzeci od te konvencije. Poziva se jednom, pri pokretanju.
/// </summary>
public static class MapsterKonfiguracija
{
    public static void Registruj()
    {
        // Kod izmjene se preskacu vrijednosti koje nisu poslane, da djelimicna
        // izmjena ne obrise polja koja korisnik nije ni dirao.
        TypeAdapterConfig.GlobalSettings.Default.IgnoreNullValues(true);

        RegistrujSifrarnike();
        RegistrujFlotu();
        RegistrujDozvole();
        RegistrujRezervacije();
        RegistrujPlacanja();
        RegistrujPrimopredaje();
    }

    /// <summary>
    /// Nazivi povezanih zapisa na DTO-ima sifrarnika.
    ///
    /// Mapster ovakvo "spljostavanje" uglavnom pogodi sam, po nazivu svojstva, ali
    /// se na to ovdje ne oslanjamo: kad bi konvencija promasila, polje bi tiho
    /// ostalo prazno i lista bi prikazivala rupe umjesto naziva. Ovako je veza
    /// izmedju entiteta i DTO-a napisana i vidljiva.
    ///
    /// Vrijednost se cita iz navigacije koju servis ucitava kroz AddInclude. Ako
    /// navigacija nije ucitana, polje ostaje prazno - zato Include i ovo idu u paru.
    /// </summary>
    private static void RegistrujSifrarnike()
    {
        TypeAdapterConfig<Grad, GradDto>.NewConfig()
            .Map(dto => dto.DrzavaNaziv, e => e.Drzava != null ? e.Drzava.Naziv : null);

        TypeAdapterConfig<Poslovnica, PoslovnicaDto>.NewConfig()
            .Map(dto => dto.GradNaziv, e => e.Grad != null ? e.Grad.Naziv : null)
            .Map(dto => dto.GradDrzavaNaziv,
                 e => e.Grad != null && e.Grad.Drzava != null ? e.Grad.Drzava.Naziv : null);

        TypeAdapterConfig<ModelVozila, ModelVozilaDto>.NewConfig()
            .Map(dto => dto.MarkaNaziv, e => e.Marka != null ? e.Marka.Naziv : null)
            .Map(dto => dto.TipVozilaNaziv, e => e.TipVozila != null ? e.TipVozila.Naziv : null)
            .Map(dto => dto.TipGorivaNaziv, e => e.TipGoriva != null ? e.TipGoriva.Naziv : null)
            .Map(dto => dto.KategorijaDozvoleOznaka,
                 e => e.KategorijaDozvole != null ? e.KategorijaDozvole.Oznaka : null);

        TypeAdapterConfig<PravilaKategorije, PravilaKategorijeDto>.NewConfig()
            .Map(dto => dto.KategorijaDozvoleOznaka,
                 e => e.KategorijaDozvole != null ? e.KategorijaDozvole.Oznaka : null)
            .Map(dto => dto.TipVozilaNaziv, e => e.TipVozila != null ? e.TipVozila.Naziv : null);
    }

    /// <summary>
    /// Vozilo nosi podatke koji zive na modelu - kubikazu, tip, marku i kategoriju
    /// dozvole. To nije dupliranje nego pogodnost za prikaz: kartica vozila u pretrazi
    /// mora pokazati sve to odjednom, a bez ovoga bi klijent za svaki red morao
    /// dohvatiti i model.
    /// </summary>
    private static void RegistrujFlotu()
    {
        TypeAdapterConfig<Vozilo, VoziloDto>.NewConfig()
            .Map(dto => dto.ModelNaziv, e => e.ModelVozila != null ? e.ModelVozila.Naziv : null)
            .Map(dto => dto.Kubikaza, e => e.ModelVozila != null ? e.ModelVozila.Kubikaza : 0)
            .Map(dto => dto.SnagaKw, e => e.ModelVozila != null ? e.ModelVozila.SnagaKw : 0)
            .Map(dto => dto.KategorijaDozvoleId,
                 e => e.ModelVozila != null ? e.ModelVozila.KategorijaDozvoleId : 0)
            .Map(dto => dto.MarkaNaziv,
                 e => e.ModelVozila != null && e.ModelVozila.Marka != null
                      ? e.ModelVozila.Marka.Naziv : null)
            .Map(dto => dto.TipVozilaNaziv,
                 e => e.ModelVozila != null && e.ModelVozila.TipVozila != null
                      ? e.ModelVozila.TipVozila.Naziv : null)
            .Map(dto => dto.TipGorivaNaziv,
                 e => e.ModelVozila != null && e.ModelVozila.TipGoriva != null
                      ? e.ModelVozila.TipGoriva.Naziv : null)
            .Map(dto => dto.KategorijaDozvoleOznaka,
                 e => e.ModelVozila != null && e.ModelVozila.KategorijaDozvole != null
                      ? e.ModelVozila.KategorijaDozvole.Oznaka : null)
            .Map(dto => dto.PoslovnicaNaziv, e => e.Poslovnica != null ? e.Poslovnica.Naziv : null)
            .Map(dto => dto.GradNaziv,
                 e => e.Poslovnica != null && e.Poslovnica.Grad != null
                      ? e.Poslovnica.Grad.Naziv : null)

            // Servis ucitava samo glavnu sliku, pa je kolekcija ovdje prazna ili ima
            // tacno jedan zapis. Vozilo bez fotografije daje prazan thumbnail, a ne gresku.
            .Map(dto => dto.ThumbnailUrl,
                 e => e.Slike.Select(s => s.PutanjaThumbnail).FirstOrDefault());

        TypeAdapterConfig<SlikaVozila, SlikaVozilaDto>.NewConfig()
            .Map(dto => dto.Url, e => e.Putanja)
            .Map(dto => dto.ThumbnailUrl, e => e.PutanjaThumbnail);

        TypeAdapterConfig<BlokadaVozila, BlokadaVozilaDto>.NewConfig()
            .Map(dto => dto.VoziloRegistarskaOznaka,
                 e => e.Vozilo != null ? e.Vozilo.RegistarskaOznaka : null)
            .Map(dto => dto.ModelNaziv,
                 e => e.Vozilo != null && e.Vozilo.ModelVozila != null
                      ? e.Vozilo.ModelVozila.Naziv : null)
            .Map(dto => dto.PoslovnicaNaziv,
                 e => e.Vozilo != null && e.Vozilo.Poslovnica != null
                      ? e.Vozilo.Poslovnica.Naziv : null)
            .Map(dto => dto.KreiraoKorisnikIme,
                 e => e.KreiraoKorisnik != null
                      ? e.KreiraoKorisnik.Ime + " " + e.KreiraoKorisnik.Prezime : null);

        TypeAdapterConfig<HistorijaStatusaRezervacije, HistorijaStatusaDto>.NewConfig()
            .Map(dto => dto.IzvrsioKorisnikIme,
                 e => e.IzvrsioKorisnik != null
                      ? e.IzvrsioKorisnik.Ime + " " + e.IzvrsioKorisnik.Prezime : null);

        TypeAdapterConfig<Rezervacija, PogodjenaRezervacijaDto>.NewConfig()
            .Map(dto => dto.KlijentImePrezime,
                 e => e.Korisnik != null ? e.Korisnik.Ime + " " + e.Korisnik.Prezime : null)
            .Map(dto => dto.KlijentEmail, e => e.Korisnik != null ? e.Korisnik.Email : null)
            .Map(dto => dto.KlijentTelefon, e => e.Korisnik != null ? e.Korisnik.Telefon : null);

        TypeAdapterConfig<Cjenovnik, CjenovnikDto>.NewConfig()
            .Map(dto => dto.ModelNaziv, e => e.ModelVozila != null ? e.ModelVozila.Naziv : null)
            .Map(dto => dto.MarkaNaziv,
                 e => e.ModelVozila != null && e.ModelVozila.Marka != null
                      ? e.ModelVozila.Marka.Naziv : null);
    }

    /// <summary>
    /// Dozvola nikad ne salje putanju do fotografije. Klijentu se javlja samo je li
    /// prilozena; sam sadrzaj ide kroz endpoint koji provjerava vlasnistvo.
    /// </summary>
    private static void RegistrujDozvole()
    {
        TypeAdapterConfig<VozackaDozvola, VozackaDozvolaDto>.NewConfig()
            .Map(dto => dto.ImaFotografiju, e => e.PutanjaSlike != null)
            .Map(dto => dto.Istekla, e => e.DatumIsteka <= DateTime.UtcNow)
            .Map(dto => dto.Kategorije,
                 e => e.Kategorije.Select(k => k.KategorijaDozvole.Oznaka).OrderBy(x => x).ToList())
            .Map(dto => dto.KategorijaIds,
                 e => e.Kategorije.Select(k => k.KategorijaDozvoleId).ToList())
            .Map(dto => dto.KlijentImePrezime,
                 e => e.Korisnik != null ? e.Korisnik.Ime + " " + e.Korisnik.Prezime : null)
            .Map(dto => dto.KlijentEmail, e => e.Korisnik != null ? e.Korisnik.Email : null)
            .Map(dto => dto.KlijentDatumRodjenja,
                 e => e.Korisnik != null ? e.Korisnik.DatumRodjenja : default(DateTime))
            .Map(dto => dto.VerifikovaoKorisnikIme,
                 e => e.VerifikovaoKorisnik != null
                      ? e.VerifikovaoKorisnik.Ime + " " + e.VerifikovaoKorisnik.Prezime : null);
    }

    /// <summary>
    /// Rezervacija nosi podatke o vozilu, klijentu i poslovnici zato sto ih kartica
    /// prikazuje odjednom. Bez toga bi lista od dvadeset rezervacija trazila dvadeset
    /// dodatnih poziva.
    /// </summary>
    private static void RegistrujRezervacije()
    {
        TypeAdapterConfig<StavkaOpreme, StavkaOpremeDto>.NewConfig()
            .Map(dto => dto.Naziv, e => e.VrstaOpreme != null ? e.VrstaOpreme.Naziv : null);

        TypeAdapterConfig<Rezervacija, RezervacijaDto>.NewConfig()
            .Map(dto => dto.KlijentImePrezime,
                 e => e.Korisnik != null ? e.Korisnik.Ime + " " + e.Korisnik.Prezime : null)
            .Map(dto => dto.KlijentEmail, e => e.Korisnik != null ? e.Korisnik.Email : null)
            .Map(dto => dto.OtkazaoKorisnikIme,
                 e => e.OtkazaoKorisnik != null
                      ? e.OtkazaoKorisnik.Ime + " " + e.OtkazaoKorisnik.Prezime : null)
            .Map(dto => dto.RazlogOtkazivanjaNaziv,
                 e => e.RazlogOtkazivanja != null ? e.RazlogOtkazivanja.Naziv : null)

            .Map(dto => dto.RegistarskaOznaka,
                 e => e.Vozilo != null ? e.Vozilo.RegistarskaOznaka : null)
            .Map(dto => dto.ModelNaziv,
                 e => e.Vozilo != null && e.Vozilo.ModelVozila != null
                      ? e.Vozilo.ModelVozila.Naziv : null)
            .Map(dto => dto.MarkaNaziv,
                 e => e.Vozilo != null && e.Vozilo.ModelVozila != null
                      && e.Vozilo.ModelVozila.Marka != null
                      ? e.Vozilo.ModelVozila.Marka.Naziv : null)
            .Map(dto => dto.ThumbnailUrl,
                 e => e.Vozilo != null
                      ? e.Vozilo.Slike.Select(s => s.PutanjaThumbnail).FirstOrDefault() : null)

            .Map(dto => dto.PoslovnicaNaziv, e => e.Poslovnica != null ? e.Poslovnica.Naziv : null)
            .Map(dto => dto.PaketOsiguranjaNaziv,
                 e => e.PaketOsiguranja != null ? e.PaketOsiguranja.Naziv : null)

            // Odbrojavanje racuna server, da ne zavisi od toga koliko je sat na
            // uredjaju tacan. Prazno je cim drzanje vise nije bitno.
            .Map(dto => dto.PreostaloSekundiDrzanja,
                 e => e.Status == StatusRezervacije.Pending
                      && e.DrziDo != null && e.DrziDo > DateTime.UtcNow
                      ? (int?)(int)(e.DrziDo.Value - DateTime.UtcNow).TotalSeconds
                      : null);
    }

    /// <summary>
    /// Placanje nosi broj i stanje rezervacije, da ekran sa placanjima ne mora za
    /// svaki red posebno dohvatati rezervaciju. Zbir vracenog racuna samo povrate
    /// koji nisu odbijeni ni ponisteni - isto pravilo koje koristi obracun otkazivanja.
    /// </summary>
    private static void RegistrujPlacanja()
    {
        TypeAdapterConfig<Refund, PovratDto>.NewConfig()
            .Map(dto => dto.KreiraoKorisnikIme,
                 e => e.KreiraoKorisnik != null
                      ? e.KreiraoKorisnik.Ime + " " + e.KreiraoKorisnik.Prezime : null);

        TypeAdapterConfig<Placanje, PlacanjeDto>.NewConfig()
            .Map(dto => dto.RezervacijaBroj, e => e.Rezervacija != null ? e.Rezervacija.Broj : null)
            .Map(dto => dto.StatusRezervacije,
                 e => e.Rezervacija != null ? e.Rezervacija.Status : default(StatusRezervacije))
            .Map(dto => dto.IsPaid, e => e.Rezervacija != null && e.Rezervacija.IsPaid)
            .Map(dto => dto.KlijentImePrezime,
                 e => e.Rezervacija != null && e.Rezervacija.Korisnik != null
                      ? e.Rezervacija.Korisnik.Ime + " " + e.Rezervacija.Korisnik.Prezime : null)
            .Map(dto => dto.UkupnoVraceno,
                 e => e.Refundi
                     .Where(r => r.Status != StatusPlacanja.Failed && r.Status != StatusPlacanja.Canceled)
                     .Sum(r => r.Iznos))
            .Map(dto => dto.Povrati, e => e.Refundi.OrderBy(r => r.Id).ToList());
    }

    /// <summary>
    /// Primopredaja salje identifikatore fotografija, ne putanje. Putanja je kljuc do
    /// privatnog fajla i ne izlazi iz servera.
    /// </summary>
    private static void RegistrujPrimopredaje()
    {
        TypeAdapterConfig<Primopredaja, PrimopredajaDto>.NewConfig()
            .Map(dto => dto.RezervacijaBroj, e => e.Rezervacija != null ? e.Rezervacija.Broj : null)
            .Map(dto => dto.IzvrsioKorisnikIme,
                 e => e.IzvrsioKorisnik != null
                      ? e.IzvrsioKorisnik.Ime + " " + e.IzvrsioKorisnik.Prezime : null)
            .Map(dto => dto.FotografijaIds, e => e.Fotografije.OrderBy(f => f.Id).Select(f => f.Id).ToList())
            .Map(dto => dto.ImaOstecenje, e => e.EvidencijaStete != null)
            .Map(dto => dto.OpisStete, e => e.EvidencijaStete != null ? e.EvidencijaStete.Opis : null)
            .Map(dto => dto.IznosStete, e => e.EvidencijaStete != null ? (decimal?)e.EvidencijaStete.Iznos : null);
    }
}
