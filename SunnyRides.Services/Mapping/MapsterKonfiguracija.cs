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
}
