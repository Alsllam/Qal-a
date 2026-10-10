using Qala.Game.Players.Application.Players.DTOs;
using Qala.Game.Players.Domain.Entities;

namespace Qala.Game.Players.Application.Players;

/// <summary>Entity → DTO maps (explicit code instead of AutoMapper; see backend/README.md).</summary>
public static class PlayerMappings
{
    public static PlayerProfileDto ToProfileDto(this Player player)
    {
        ArgumentNullException.ThrowIfNull(player);
        return new PlayerProfileDto
        {
            Id = player.Id,
            UserId = player.UserId,
            DisplayName = player.DisplayName,
            AvatarId = player.AvatarId,
            Locale = player.Locale,
            Rating = (int)Math.Round(player.Rating),
            RatingDeviation = (int)Math.Round(player.RatingDeviation),
            GamesPlayed = player.GamesPlayed,
            Wins = player.Wins,
            Losses = player.Losses,
            Draws = player.Draws,
        };
    }

    public static PlayerDto ToDto(this Player player)
    {
        ArgumentNullException.ThrowIfNull(player);
        return new PlayerDto
        {
            Id = player.Id,
            UserId = player.UserId,
            DisplayName = player.DisplayName,
            AvatarId = player.AvatarId,
            Locale = player.Locale,
            Rating = player.Rating,
            RatingDeviation = player.RatingDeviation,
            Volatility = player.Volatility,
            GamesPlayed = player.GamesPlayed,
            Wins = player.Wins,
            Losses = player.Losses,
            Draws = player.Draws,
            IsActive = player.IsActive,
            CreationTime = player.CreationTime,
            LastGameAt = player.LastGameAt,
        };
    }

    public static PlayerListDto ToListDto(this Player player)
    {
        ArgumentNullException.ThrowIfNull(player);
        return new PlayerListDto
        {
            Id = player.Id,
            UserId = player.UserId,
            DisplayName = player.DisplayName,
            Rating = (int)Math.Round(player.Rating),
            GamesPlayed = player.GamesPlayed,
            IsActive = player.IsActive,
            CreationTime = player.CreationTime,
        };
    }
}
