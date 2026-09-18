namespace SunnyRides.Model.Poruke;

/// <summary>
/// Razmjene na RabbitMQ-u.
///
/// Red ima jednog citaoca: poruku uzme prvi ko stigne i ona je time potrosena. To je
/// tacno ono sto treba za posao koji se radi jednom, kao slanje emaila.
///
/// Za guranje obavjestenja u aplikaciju treba suprotno - poruku mora dobiti **svaka**
/// pokrenuta instanca API-ja, jer svaka drzi svoje otvorene veze prema uredjajima.
/// Zato ovdje stoji fanout razmjena: svaka instanca sebi pravi privatni red, veze ga
/// na razmjenu i dobija kopiju svake poruke.
/// </summary>
public static class Razmjene
{
    /// <summary>Obavjestenja koja se u realnom vremenu guraju korisniku kroz SignalR.</summary>
    public const string Notifikacije = "notifikacije";
}
