using SunnyRides.Model.Enums;

namespace SunnyRides.Model.DTOs;

/// <summary>
/// Jedno preporuceno vozilo, zajedno sa razlogom zbog kojeg je predlozeno.
///
/// Obje komponente skora se vracaju odvojeno, a ne samo konacni broj. Tako se na
/// odbrani moze pokazati zasto je bas ovo vozilo prvo, i vidi se je li ga gore
/// doveo profil korisnika ili sama popularnost.
/// </summary>
public class PreporukaDto
{
    public VoziloDto Vozilo { get; set; } = null!;

    /// <summary>Konacni skor, 0-1.</summary>
    public double Skor { get; set; }

    /// <summary>Cime je stavka izracunata - predikcijom modela ili rezervnim pravilom.</summary>
    public MetodaPreporuke Metoda { get; set; }

    /// <summary>
    /// Ocjena koju model predvidja da bi korisnik dao ovom vozilu, na skali 1-5.
    /// Prazno kad je stavka izracunata rezervnim putem.
    /// </summary>
    public double? PredvidjenaOcjena { get; set; }

    /// <summary>Poklapanje sa profilom korisnika, 0-1. Popunjeno samo na rezervnom putu.</summary>
    public double Slicnost { get; set; }

    /// <summary>Popularnost vozila u floti, 0-1. Popunjeno samo na rezervnom putu.</summary>
    public double Popularnost { get; set; }

    /// <summary>
    /// Recenica koja objasnjava preporuku. Nije ukrasni tekst - gradi se od signala
    /// koji je stvarno najvise doprinio skoru.
    /// </summary>
    public string Obrazlozenje { get; set; } = null!;
}
