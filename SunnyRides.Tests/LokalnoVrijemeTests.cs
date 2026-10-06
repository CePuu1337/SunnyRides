using SunnyRides.Services.Vrijeme;
using Xunit;

namespace SunnyRides.Tests;

/// <summary>
/// Vremena u porukama: u bazi su UTC, a klijent cita lokalno vrijeme agencije, sa
/// ljetnim i zimskim racunanjem vremena.
/// </summary>
public class LokalnoVrijemeTests
{
    [Fact]
    public void Ljeti_je_lokalno_vrijeme_dva_sata_ispred_utc()
    {
        var utc = new DateTime(2026, 9, 30, 14, 0, 0, DateTimeKind.Utc);

        Assert.Equal("30.09.2026. 16:00", LokalnoVrijeme.DatumIVrijeme(utc));
    }

    [Fact]
    public void Zimi_je_lokalno_vrijeme_sat_ispred_utc()
    {
        var utc = new DateTime(2026, 12, 15, 14, 0, 0, DateTimeKind.Utc);

        Assert.Equal("15.12.2026. 15:00", LokalnoVrijeme.DatumIVrijeme(utc));
    }

    [Fact]
    public void Kasno_uvece_po_utc_je_vec_sljedeci_dan_lokalno()
    {
        var utc = new DateTime(2026, 7, 1, 23, 30, 0, DateTimeKind.Utc);

        Assert.Equal("02.07.2026. u 01:30", LokalnoVrijeme.DatumUVrijeme(utc));
    }

    [Fact]
    public void Vrijeme_iz_baze_bez_oznake_vrste_se_cita_kao_utc()
    {
        // EF vraca DateTime sa Kind = Unspecified; i takvo vrijeme je u bazi UTC.
        var izBaze = new DateTime(2026, 9, 30, 14, 0, 0, DateTimeKind.Unspecified);

        Assert.Equal("16:00", LokalnoVrijeme.Vrijeme(izBaze));
    }
}
