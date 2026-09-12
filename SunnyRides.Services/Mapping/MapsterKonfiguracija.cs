using Mapster;
using SunnyRides.Model.DTOs;
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
    }
}
