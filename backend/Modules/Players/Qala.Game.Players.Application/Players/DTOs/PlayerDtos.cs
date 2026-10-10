using System.Text.Json.Serialization;
using Qala.Framework.Application.Dtos;

namespace Qala.Game.Players.Application.Players.DTOs;

/// <summary>The caller's own profile (<c>POST me</c>).</summary>
public sealed class PlayerProfileDto
{
    public Guid Id { get; init; }

    /// <summary>The auth user id; the id used in matches and events.</summary>
    public Guid UserId { get; init; }

    public string DisplayName { get; init; } = string.Empty;

    public string? AvatarId { get; init; }

    public string Locale { get; init; } = string.Empty;

    public int Rating { get; init; }

    public int RatingDeviation { get; init; }

    public int GamesPlayed { get; init; }

    public int Wins { get; init; }

    public int Losses { get; init; }

    public int Draws { get; init; }
}

/// <summary><c>PUT me</c>.</summary>
public sealed class UpdateMyProfileDto : ISkipAutoValidation
{
    public string DisplayName { get; set; } = string.Empty;

    public string? AvatarId { get; set; }

    public string Locale { get; set; } = string.Empty;

    [JsonIgnore]
    public bool SkipAutoValidations { get; set; } = true;
}

/// <summary>Admin view of a player (<c>POST getbyid</c>).</summary>
public sealed class PlayerDto
{
    public Guid Id { get; init; }

    public Guid UserId { get; init; }

    public string DisplayName { get; init; } = string.Empty;

    public string? AvatarId { get; init; }

    public string Locale { get; init; } = string.Empty;

    public double Rating { get; init; }

    public double RatingDeviation { get; init; }

    public double Volatility { get; init; }

    public int GamesPlayed { get; init; }

    public int Wins { get; init; }

    public int Losses { get; init; }

    public int Draws { get; init; }

    public bool IsActive { get; init; }

    public DateTime CreationTime { get; init; }

    public DateTime? LastGameAt { get; init; }
}

/// <summary>A row of <c>POST list</c>.</summary>
public sealed class PlayerListDto
{
    public Guid Id { get; init; }

    public Guid UserId { get; init; }

    public string DisplayName { get; init; } = string.Empty;

    public int Rating { get; init; }

    public int GamesPlayed { get; init; }

    public bool IsActive { get; init; }

    public DateTime CreationTime { get; init; }
}

/// <summary><c>POST list</c> filter: free text on the display name, active filter, rating range.</summary>
public sealed class FilterPlayerDto : BaseFilterRequestDto
{
    public double? MinRating { get; set; }

    public double? MaxRating { get; set; }
}

/// <summary>A row of <c>POST leaderboard</c>.</summary>
public sealed record LeaderboardEntryDto(int Rank, Guid PlayerId, string DisplayName, int Rating, int GamesPlayed);
