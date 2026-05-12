namespace AlfaCore.Models;

public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items      { get; init; } = [];
    public int              Total      { get; init; }
    public int              PageNumber { get; init; }
    public int              PageSize   { get; init; }
    public int  TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)Total / PageSize) : 0;
    public bool HasPrev    => PageNumber > 1;
    public bool HasNext    => PageNumber < TotalPages;
}
