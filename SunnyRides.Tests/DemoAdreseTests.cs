using SunnyRides.Services.Poruke;
using Xunit;

namespace SunnyRides.Tests;

/// <summary>
/// Worker preusmjerava samo mail na rezervisane demo domene. Stvarna adresa mora dobiti
/// svoj mail, inace profesor koji se registruje svojim emailom ne bi dobio nijedan.
/// </summary>
public class DemoAdreseTests
{
    [Theory]
    [InlineData("mobile@sunnyrides.example")]
    [InlineData("dino.ramic@example.com")]
    [InlineData("neko@EXAMPLE.ORG")]
    [InlineData("neko@mail.example.net")]
    [InlineData("neko@nesto.test")]
    [InlineData("neko@nesto.invalid")]
    public void Rezervisane_domene_su_demo(string adresa)
    {
        Assert.True(DemoAdrese.JeDemo(adresa));
    }

    [Theory]
    [InlineData("ammarpuce2003@gmail.com")]
    [InlineData("ime.prezime@edu.fit.ba")]
    [InlineData("neko@examples.com")]
    [InlineData("neko@myexample.com")]
    [InlineData("neko@test.ba")]
    public void Stvarne_adrese_nisu_demo(string adresa)
    {
        Assert.False(DemoAdrese.JeDemo(adresa));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("bez-majmuna")]
    [InlineData("neko@")]
    public void Neispravna_adresa_nije_demo(string? adresa)
    {
        Assert.False(DemoAdrese.JeDemo(adresa));
    }
}
