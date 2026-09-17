using SunnyRides.Services.Cijene;

namespace SunnyRides.Services.Primopredaje;

public record UlazPovrata(
    DateTime UgovorenoVracanje,
    DateTime DatumPovrata,
    decimal DnevnaCijena,
    decimal UplaceniDepozit,
    decimal IznosStete);

public record RezultatPovrata(
    int KasnjenjeMinuta,
    bool UnutarTolerancije,
    int DanaPrekoracenja,
    decimal Doplata,
    decimal IznosStete,
    decimal ZadrzanoOdDepozita,
    decimal PovratDepozita,
    decimal NepokrivenoDepozitom,
    string Obrazlozenje);

/// <summary>
/// Obracun depozita pri povratu vozila: uplaceni depozit, minus steta, minus doplata
/// za kasnjenje.
///
/// Nema bazu ni zavisnosti, pa se provjerava obicnim testovima. Kasnjenje se broji
/// istim pravilom kao trajanje najma: do 59 minuta se ne naplacuje, a svaki zapoceti
/// dan preko toga je cijeli dan. Da ovdje vazi drugo pravilo, klijent bi za isto
/// prekoracenje platio razlicito zavisno od toga da li je produzio najam ili samo
/// kasnio.
/// </summary>
public static class ObracunPovrata
{
    public static RezultatPovrata Izracunaj(UlazPovrata ulaz)
    {
        var kasnjenje = ulaz.DatumPovrata - ulaz.UgovorenoVracanje;
        if (kasnjenje < TimeSpan.Zero)
        {
            kasnjenje = TimeSpan.Zero;
        }

        var tolerancija = TimeSpan.FromMinutes(TrajanjeNajma.GraceMinuta);
        var unutarTolerancije = kasnjenje <= tolerancija;

        var dana = 0;
        if (!unutarTolerancije)
        {
            var puniDani = (int)Math.Floor(kasnjenje.TotalHours / 24);
            var ostatak = kasnjenje - TimeSpan.FromHours(puniDani * 24);
            dana = ostatak > tolerancija ? puniDani + 1 : puniDani;
        }

        var doplata = Zaokruzi(dana * ulaz.DnevnaCijena);
        var steta = Zaokruzi(Math.Max(ulaz.IznosStete, 0m));
        var depozit = Zaokruzi(Math.Max(ulaz.UplaceniDepozit, 0m));

        var zaNaplatu = steta + doplata;
        var zadrzano = Math.Min(depozit, zaNaplatu);

        return new RezultatPovrata(
            KasnjenjeMinuta: (int)Math.Floor(kasnjenje.TotalMinutes),
            UnutarTolerancije: unutarTolerancije,
            DanaPrekoracenja: dana,
            Doplata: doplata,
            IznosStete: steta,
            ZadrzanoOdDepozita: zadrzano,
            PovratDepozita: depozit - zadrzano,
            NepokrivenoDepozitom: zaNaplatu - zadrzano,
            Obrazlozenje: Obrazlozi(kasnjenje, unutarTolerancije, dana, doplata, steta, depozit - zadrzano));
    }

    private static string Obrazlozi(
        TimeSpan kasnjenje, bool unutarTolerancije, int dana, decimal doplata, decimal steta, decimal povrat)
    {
        var dijelovi = new List<string>();

        if (kasnjenje == TimeSpan.Zero)
        {
            dijelovi.Add("Vozilo vraceno na vrijeme.");
        }
        else if (unutarTolerancije)
        {
            dijelovi.Add($"Kasnjenje od {(int)kasnjenje.TotalMinutes} min je unutar dozvoljenih {TrajanjeNajma.GraceMinuta} min i ne naplacuje se.");
        }
        else
        {
            dijelovi.Add($"Kasnjenje od {FormatirajTrajanje(kasnjenje)} naplacuje se kao {dana} {(dana == 1 ? "dan" : "dana")}: {doplata:0.00} EUR.");
        }

        dijelovi.Add(steta > 0 ? $"Steta: {steta:0.00} EUR." : "Bez stete.");
        dijelovi.Add($"Povrat depozita: {povrat:0.00} EUR.");

        return string.Join(" ", dijelovi);
    }

    private static string FormatirajTrajanje(TimeSpan trajanje) =>
        trajanje.TotalHours >= 1
            ? $"{(int)trajanje.TotalHours} h {trajanje.Minutes} min"
            : $"{trajanje.Minutes} min";

    private static decimal Zaokruzi(decimal iznos) =>
        Math.Round(iznos, 2, MidpointRounding.AwayFromZero);
}
