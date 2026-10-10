using System.Text.Json.Serialization;
using Qala.Framework.Domain.Constants;

namespace Qala.Framework.Application.Dtos;

/// <summary><c>{ id }</c> body for get-by-id, delete, activate and deactivate.</summary>
public class EntityIdDto<TKey>
{
    public TKey Id { get; set; } = default!;
}

/// <summary>Paging and sorting.</summary>
public class PagedRequestDto
{
    public int SkipCount { get; set; }

    public int MaxResultCount { get; set; } = FieldDefinitions.DefaultPageSize;

    /// <summary>Dynamic LINQ sort, e.g. <c>"CreationTime Desc"</c>.</summary>
    public string? Sorting { get; set; }

    /// <summary>Clamps paging values to safe ranges.</summary>
    public void Normalize(string defaultSorting = FieldDefinitions.DefaultSorting)
    {
        SkipCount = Math.Max(0, SkipCount);
        MaxResultCount = Math.Clamp(MaxResultCount, 1, FieldDefinitions.MaxPageSize);
        Sorting = string.IsNullOrWhiteSpace(Sorting) ? defaultSorting : Sorting;
    }
}

public enum ActiveFilter
{
    All = 0,
    Active = 1,
    InActive = 2,
}

/// <summary>Base of every <c>Filter{X}Dto</c>: paging, sorting, free text and active filter.</summary>
public class BaseFilterRequestDto : PagedRequestDto
{
    public string? FilterText { get; set; }

    public ActiveFilter? ActiveFilter { get; set; }
}

/// <summary>A dropdown row.</summary>
public class BaseLookupResponseDto<TKey>
{
    public TKey Id { get; set; } = default!;

    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Create/Update DTOs implement this so FluentValidation's automatic validation skips them; the AppService validates
/// explicitly (once) after setting <see cref="SkipAutoValidations"/> to false.
/// </summary>
public interface ISkipAutoValidation
{
    [JsonIgnore]
    bool SkipAutoValidations { get; set; }
}
