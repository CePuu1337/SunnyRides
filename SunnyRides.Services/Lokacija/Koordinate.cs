using SunnyRides.Services.Exceptions;

namespace SunnyRides.Services.Lokacija;

/// <summary>Provjere koje vaze za svaki par koordinata - poslovnice, grada i telefona.</summary>
public static class Koordinate
{
    /// <summary>
    /// Koordinate su opcione, ali idu u paru. Tacka sa samo jednom koordinatom se ne
    /// moze ni prikazati ni mjeriti, a podatak izgleda kao da postoji.
    /// </summary>
    public static void ProvjeriPar(double? latituda, double? longituda)
    {
        if (latituda.HasValue != longituda.HasValue)
        {
            throw new BusinessException(
                "Koordinate se unose zajedno - potrebne su i latituda i longituda, ili nijedna.");
        }

        if (latituda is < -90 or > 90 || longituda is < -180 or > 180)
        {
            throw new BusinessException(
                "Koordinate nisu ispravne. Latituda ide od -90 do 90, a longituda od -180 do 180.");
        }
    }
}
