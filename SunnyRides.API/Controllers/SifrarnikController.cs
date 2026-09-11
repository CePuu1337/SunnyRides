using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SunnyRides.Model.Konstante;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Base;

namespace SunnyRides.API.Controllers;

/// <summary>
/// Zajednicka baza za sve sifrarnike, sa jednom razlikom u odnosu na obican CRUD
/// kontroler: citanje i pisanje nemaju istu zastitu.
///
/// Citanje nasljedjuje [Authorize] sa <see cref="BaseController{TModel, TSearch}"/>,
/// pa ga smije svaki prijavljen korisnik. To nije popustanje, nego posljedica toga
/// cemu sifrarnici sluze - klijent u mobilnoj aplikaciji bira poslovnicu preuzimanja
/// i tip vozila, i te liste mora odnekud dobiti. Sadrzaj je ionako javan podatak
/// agencije, isti za sve.
///
/// Pisanje je administratorsko. Odrzavanje sifrarnika je modul koji prema
/// specifikaciji ne vidi ni uposlenik, pa POST, PUT i DELETE traze ulogu
/// Administrator. Atributi stoje ovdje, na jednom mjestu, umjesto da se ponavljaju
/// na jedanaest kontrolera gdje bi se na dvanaestom zaboravili.
/// </summary>
public abstract class SifrarnikController<TModel, TSearch, TInsert, TUpdate>
    : BaseCRUDController<TModel, TSearch, TInsert, TUpdate>
    where TSearch : BaseSearchObject
{
    protected SifrarnikController(ICRUDService<TModel, TSearch, TInsert, TUpdate> service)
        : base(service)
    {
    }

    [Authorize(Roles = Uloge.Administrator)]
    public override Task<TModel> InsertAsync([FromBody] TInsert request, CancellationToken ct)
        => base.InsertAsync(request, ct);

    [Authorize(Roles = Uloge.Administrator)]
    public override Task<TModel> UpdateAsync(int id, [FromBody] TUpdate request, CancellationToken ct)
        => base.UpdateAsync(id, request, ct);

    [Authorize(Roles = Uloge.Administrator)]
    public override Task<IActionResult> DeleteAsync(int id, CancellationToken ct)
        => base.DeleteAsync(id, ct);
}
