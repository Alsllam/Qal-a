using Qala.Framework.Domain.Entities;

namespace Qala.Game.Matches.Domain.Entities;

/// <summary>
/// Read model of a player's rating and name, kept up to date from <c>PlayerRatingChangedEto</c>. Used for matchmaking
/// and for the player snapshot stored on a new match. <see cref="Entity{TKey}.Id"/> is the auth user id.
/// </summary>
public class PlayerRating : Entity<Guid>
{
    protected PlayerRating()
    {
    }

    public PlayerRating(Guid userId, string displayName, double rating, double ratingDeviation, DateTime updatedAt)
        : base(userId) => Update(displayName, rating, ratingDeviation, updatedAt);

    public string DisplayName { get; private set; } = string.Empty;

    public double Rating { get; private set; }

    public double RatingDeviation { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public void Update(string displayName, double rating, double ratingDeviation, DateTime updatedAt)
    {
        DisplayName = displayName;
        Rating = rating;
        RatingDeviation = ratingDeviation;
        UpdatedAt = updatedAt;
    }
}
