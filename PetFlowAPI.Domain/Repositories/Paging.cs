namespace PetFlowAPI.Domain.Repositories;

/// <summary>Parâmetros de paginação/ordenação (página base 0, como na Sprint 3).</summary>
public sealed record PageRequest(int Page = 0, int Size = 10, string? SortBy = null, bool Descending = false)
{
    public const int MaxSize = 100;
}

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int Size, long TotalItems)
{
    public int TotalPages => Size <= 0 ? 0 : (int)Math.Ceiling(TotalItems / (double)Size);

    public PagedResult<TOut> Map<TOut>(Func<T, TOut> selector)
        => new(Items.Select(selector).ToList(), Page, Size, TotalItems);
}
