namespace SunnyRides.Model.DTOs;

/// <summary>Uloga u sistemu. Popunjava padajucu listu pri dodjeli uloga korisniku.</summary>
public class RoleDto
{
    public int Id { get; set; }
    public string Naziv { get; set; } = null!;
    public string? Opis { get; set; }
}
