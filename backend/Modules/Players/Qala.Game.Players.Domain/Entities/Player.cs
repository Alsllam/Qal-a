using Qala.Framework.Domain.Entities;
using Qala.Game.Players.Domain.Ratings;

namespace Qala.Game.Players.Domain.Entities;

/// <summary>A player profile with its Glicko-2 rating. <see cref="UserId"/> is the auth user id (<c>sub</c>).</summary>
public class Player : FullAuditedEntity<Guid>, IActivableEntity, IHasConcurrencyStamp
{
    protected Player()
    {
    }

    public Player(Guid id, Guid userId, string displayName, string locale)
        : base(id)
    {
        UserId = userId;
        DisplayName = displayName;
        Locale = locale;
        IsActive = true;
        Rating = Glicko2Rating.Initial.Rating;
        RatingDeviation = Glicko2Rating.Initial.Deviation;
        Volatility = Glicko2Rating.Initial.Volatility;
    }

    public Guid UserId { get; private set; }

    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>One of the app's built-in avatars.</summary>
    public string? AvatarId { get; private set; }

    /// <summary><c>ar</c> or <c>en</c>.</summary>
    public string Locale { get; private set; } = string.Empty;

    public double Rating { get; private set; }

    public double RatingDeviation { get; private set; }

    public double Volatility { get; private set; }

    public int GamesPlayed { get; private set; }

    public int Wins { get; private set; }

    public int Losses { get; private set; }

    public int Draws { get; private set; }

    public DateTime? LastGameAt { get; private set; }

    /// <summary>False when banned.</summary>
    public bool IsActive { get; set; }

    public string ConcurrencyStamp { get; set; } = string.Empty;

    public Glicko2Rating Glicko => new(Rating, RatingDeviation, Volatility);

    public void UpdateProfile(string displayName, string? avatarId, string locale)
    {
        DisplayName = displayName;
        AvatarId = avatarId;
        Locale = locale;
    }

    /// <summary>Records a rated game: <paramref name="score"/> is 1 (win), 0.5 (draw) or 0 (loss).</summary>
    public void ApplyRatedGame(Glicko2Rating newRating, double score, DateTime playedAt)
    {
        ArgumentNullException.ThrowIfNull(newRating);
        Rating = newRating.Rating;
        RatingDeviation = newRating.Deviation;
        Volatility = newRating.Volatility;
        GamesPlayed++;
        if (score > 0.5)
        {
            Wins++;
        }
        else if (score < 0.5)
        {
            Losses++;
        }
        else
        {
            Draws++;
        }

        LastGameAt = playedAt;
    }
}

/// <summary>A match already applied to ratings. Its id is the match id, so a redelivered event is ignored.</summary>
public class RatedMatch : Entity<Guid>
{
    protected RatedMatch()
    {
    }

    public RatedMatch(Guid matchId, DateTime processedAt)
        : base(matchId) => ProcessedAt = processedAt;

    public DateTime ProcessedAt { get; private set; }
}
