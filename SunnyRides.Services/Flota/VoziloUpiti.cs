using Microsoft.EntityFrameworkCore;
using SunnyRides.Services.Database.Entities;

namespace SunnyRides.Services.Flota;

/// <summary>Ucitavanje vozila za prikaz, na jednom mjestu za sve servise koji ga vracaju.</summary>
public static class VoziloUpiti
{
    /// <summary>
    /// Sve sto kartica vozila prikazuje: marka, model, tip, gorivo, kategorija dozvole,
    /// poslovnica sa gradom i samo glavna fotografija - ostale ostaju na serveru dok ih
    /// neko ne zatrazi kroz /api/vozila/{id}/slike.
    /// </summary>
    public static IQueryable<Vozilo> SaPovezanim(this IQueryable<Vozilo> upit) =>
        upit.Include(x => x.ModelVozila).ThenInclude(m => m.Marka)
            .Include(x => x.ModelVozila).ThenInclude(m => m.TipVozila)
            .Include(x => x.ModelVozila).ThenInclude(m => m.TipGoriva)
            .Include(x => x.ModelVozila).ThenInclude(m => m.KategorijaDozvole)
            .Include(x => x.Poslovnica).ThenInclude(p => p.Grad)
            .Include(x => x.Slike.Where(s => s.JeGlavna));
}
