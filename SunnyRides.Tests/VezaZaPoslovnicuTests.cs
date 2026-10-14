using SunnyRides.Model.Konstante;
using SunnyRides.Services.Auth;

namespace SunnyRides.Tests;

public class VezaZaPoslovnicuTests
{
    [Fact]
    public void Uposlenik_je_vezan_za_poslovnicu()
    {
        Assert.True(VezaZaPoslovnicu.JeVezan(new[] { Uloge.Uposlenik }));
    }

    [Fact]
    public void Administrator_nije_vezan_ni_kad_je_i_uposlenik()
    {
        Assert.False(VezaZaPoslovnicu.JeVezan(new[] { Uloge.Administrator }));
        Assert.False(VezaZaPoslovnicu.JeVezan(new[] { Uloge.Uposlenik, Uloge.Administrator }));
    }

    [Fact]
    public void Klijent_i_nalog_bez_uloga_nisu_vezani()
    {
        Assert.False(VezaZaPoslovnicu.JeVezan(new[] { Uloge.Klijent }));
        Assert.False(VezaZaPoslovnicu.JeVezan(Array.Empty<string>()));
    }
}
