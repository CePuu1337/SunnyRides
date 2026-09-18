using System.ComponentModel.DataAnnotations;

namespace SunnyRides.Model.Requests;

/// <summary>Nova lista uloga korisnika. Salje se cijela, ne kao dodavanje jedne po jedne.</summary>
public class UlogeKorisnikaRequest
{
    [MinLength(1, ErrorMessage = "Korisnik mora imati najmanje jednu ulogu.")]
    public List<int> UlogeIds { get; set; } = new();
}
