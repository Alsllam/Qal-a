namespace Qala.Framework.Domain.Paging;

/// <summary>One page of results plus the total number of matching rows.</summary>
public class PagedResultDto<T>
{
    public PagedResultDto()
    {
    }

    public PagedResultDto(IReadOnlyList<T> items, long totalCount)
    {
        Items = items;
        TotalCount = totalCount;
    }

    public IReadOnlyList<T> Items { get; set; } = [];

    public long TotalCount { get; set; }

    /// <summary>Maps the items, keeping the total.</summary>
    public PagedResultDto<TOut> Map<TOut>(Func<T, TOut> selector) => new(Items.Select(selector).ToList(), TotalCount);
}
