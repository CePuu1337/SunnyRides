using Microsoft.AspNetCore.Mvc;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Sifrarnici;

namespace SunnyRides.API.Controllers;

/// <summary>Sta koja kategorija dozvole obuhvata.</summary>
[Route("api/pravila-kategorija")]
public class PravilaKategorijeController
    : SifrarnikController<PravilaKategorijeDto, PravilaKategorijeSearchObject, PravilaKategorijeInsertRequest, PravilaKategorijeUpdateRequest>
{
    public PravilaKategorijeController(IPravilaKategorijeService service) : base(service)
    {
    }
}
