namespace SunnyRides.Services.Cijene;

/// <summary>Jedna stavka opreme, sa cijenom kakva je vazila u trenutku obracuna.</summary>
public record StavkaOpremeUlaz(
    int VrstaOpremeId,
    string Naziv,
    int Kolicina,
    decimal? CijenaPoDanu,
    decimal? FiksnaCijena);

/// <summary>
/// Sve sto obracunu treba, vec procitano iz baze.
///
/// Racunanje ne zna odakle su ove vrijednosti dosle - to je posao servisa. Zbog
/// toga se cijeli obracun moze provjeriti bez baze, bez mokova i bez konteksta.
/// </summary>
public record UlazObracuna(
    DateTime DatumOd,
    DateTime DatumDo,
    decimal SatnaTarifa,
    decimal DnevnaTarifa,
    decimal Mnozilac,
    string? NazivSezone,
    int PopustPrag1,
    decimal PopustProcenat1,
    int PopustPrag2,
    decimal PopustProcenat2,
    decimal IznosDepozita,
    IReadOnlyList<StavkaOpremeUlaz> Oprema,
    int? PaketOsiguranjaId,
    string? PaketOsiguranjaNaziv,
    decimal OsiguranjeCijenaPoDanu);
