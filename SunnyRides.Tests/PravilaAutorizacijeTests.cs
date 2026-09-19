using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using SunnyRides.API.Controllers;
using SunnyRides.Model.Konstante;

namespace SunnyRides.Tests;

/// <summary>
/// Provjere uloga na kontrolerima.
///
/// ASP.NET Core atribute [Authorize] sa klase i sa metode ne zamjenjuje nego sabira:
/// zahtjev mora proci sve. Metoda zato ne moze prosiriti pristup koji je klasa suzila -
/// ako klasa trazi administratora, a metoda kaze "administrator ili uposlenik",
/// uposlenik je i dalje odbijen. Kod izgleda kao da radi, a ne radi, i greska se vidi
/// tek kad se neko prijavi sa tom ulogom.
/// </summary>
public class PravilaAutorizacijeTests
{
    private static readonly Assembly Api = typeof(KlijentController).Assembly;

    [Fact]
    public void Metoda_ne_navodi_ulogu_koju_klasa_ionako_odbija()
    {
        var problemi = new List<string>();

        foreach (var kontroler in Kontroleri())
        {
            var dozvoljenoKlasom = DozvoljeneUloge(kontroler.GetCustomAttributes<AuthorizeAttribute>(inherit: true));

            if (dozvoljenoKlasom is null)
            {
                continue;
            }

            foreach (var metoda in Akcije(kontroler))
            {
                foreach (var atribut in metoda.GetCustomAttributes<AuthorizeAttribute>(inherit: true))
                {
                    foreach (var uloga in Uloge(atribut))
                    {
                        if (!dozvoljenoKlasom.Contains(uloga))
                        {
                            problemi.Add($"{kontroler.Name}.{metoda.Name} navodi ulogu '{uloga}', a klasa je odbija.");
                        }
                    }
                }
            }
        }

        Assert.True(problemi.Count == 0, string.Join(Environment.NewLine, problemi));
    }

    [Fact]
    public void Pretragu_klijenata_smije_i_uposlenik()
    {
        var uloge = DozvoljeneUloge(
            typeof(KlijentController).GetCustomAttributes<AuthorizeAttribute>(inherit: true));

        Assert.NotNull(uloge);
        Assert.Contains(Model.Konstante.Uloge.Uposlenik, uloge);
        Assert.Contains(Model.Konstante.Uloge.Administrator, uloge);
        Assert.DoesNotContain(Model.Konstante.Uloge.Klijent, uloge);
    }

    [Fact]
    public void Upravljanje_nalozima_ostaje_samo_administratoru()
    {
        var uloge = DozvoljeneUloge(
            typeof(KorisnikController).GetCustomAttributes<AuthorizeAttribute>(inherit: true));

        Assert.NotNull(uloge);
        Assert.Equal(new[] { Model.Konstante.Uloge.Administrator }, uloge.ToArray());
    }

    // --- pomocne ----------------------------------------------------------

    private static IEnumerable<Type> Kontroleri() =>
        Api.GetTypes().Where(x => !x.IsAbstract && typeof(ControllerBase).IsAssignableFrom(x));

    private static IEnumerable<MethodInfo> Akcije(Type kontroler) =>
        kontroler
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(x => x.GetCustomAttributes<HttpMethodAttribute>(inherit: true).Any());

    /// <summary>
    /// Uloge koje prolaze sve atribute sa ulogama. Unutar jednog atributa uloge se
    /// odvajaju zarezom i dovoljna je jedna; izmedju atributa moraju proci svi, pa je
    /// rezultat presjek. Prazno znaci da atributi uloge ne ogranicavaju.
    /// </summary>
    private static HashSet<string>? DozvoljeneUloge(IEnumerable<AuthorizeAttribute> atributi)
    {
        HashSet<string>? rezultat = null;

        foreach (var atribut in atributi.Where(x => !string.IsNullOrWhiteSpace(x.Roles)))
        {
            var uloge = Uloge(atribut).ToHashSet();

            if (rezultat is null)
            {
                rezultat = uloge;
            }
            else
            {
                rezultat.IntersectWith(uloge);
            }
        }

        return rezultat;
    }

    private static IEnumerable<string> Uloge(AuthorizeAttribute atribut) =>
        (atribut.Roles ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
