using SunnyRides.Model.Enums;

namespace SunnyRides.Services.Placanja;

/// <summary>
/// Pretvaranje iznosa i statusa izmedju Stripe-a i nase baze.
///
/// Stripe iznose drzi kao cijeli broj u najmanjoj jedinici valute (centima), a mi
/// kao decimal u eurima. Pretvaranje je na jednom mjestu, bez zavisnosti, da se moze
/// testirati - greska za jedan cent ovdje znaci da se naplaceno i ocekivano nikad ne
/// poklope.
/// </summary>
public static class IznosiStripe
{
    public static long UCente(decimal iznos) =>
        (long)Math.Round(iznos * 100m, 0, MidpointRounding.AwayFromZero);

    public static decimal IzCenti(long centi) => centi / 100m;

    /// <summary>
    /// Status PaymentIntent-a preveden u nas enum.
    ///
    /// "requires_payment_method" poslije neuspjelog pokusaja i dalje znaci da se moze
    /// platiti istim intentom, pa se ne proglasava konacnim neuspjehom - klijent smije
    /// pokusati drugom karticom dok drzanje traje.
    /// </summary>
    public static StatusPlacanja StatusIntenta(string? status) => status switch
    {
        "succeeded" => StatusPlacanja.Succeeded,
        "canceled" => StatusPlacanja.Canceled,
        "processing" or "requires_action" or "requires_capture" => StatusPlacanja.Pending,
        _ => StatusPlacanja.Created
    };

    public static StatusPlacanja StatusPovrata(string? status) => status switch
    {
        "succeeded" => StatusPlacanja.Succeeded,
        "failed" => StatusPlacanja.Failed,
        "canceled" => StatusPlacanja.Canceled,
        _ => StatusPlacanja.Pending
    };

    /// <summary>Placanje koje jos moze zavrsiti uspjehom - intent se ponovo koristi umjesto novog.</summary>
    public static bool JeOtvoreno(StatusPlacanja status) =>
        status is StatusPlacanja.Created or StatusPlacanja.Pending;

    /// <summary>
    /// Povrat koji se racuna kao vracen novac. Neuspio i ponisten ne racuna se -
    /// taj novac je i dalje kod agencije.
    /// </summary>
    public static bool PovratJeVazeci(StatusPlacanja status) =>
        status is not (StatusPlacanja.Failed or StatusPlacanja.Canceled);
}
