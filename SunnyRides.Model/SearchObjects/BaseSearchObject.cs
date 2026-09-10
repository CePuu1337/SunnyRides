namespace SunnyRides.Model.SearchObjects;

public class BaseSearchObject
{
    public int? Page { get; set; }
    public int? PageSize { get; set; }
    public string? OrderBy { get; set; }
    public bool IncludeTotalCount { get; set; } = true;
}
