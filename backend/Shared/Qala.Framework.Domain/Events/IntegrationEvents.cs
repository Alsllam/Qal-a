namespace Qala.Framework.Domain.Events;

// Events consumed by more than one module live here (see docs/architecture.md §5 "Events").
// Player ids in events are the auth user ids (the "sub" claim), which both modules share.

/// <summary>Published by Matches when a rated game ends. Consumed by Players (ratings) and analytics.</summary>
/// <param name="Winner"><c>south</c>, <c>north</c> or null for a draw.</param>
/// <param name="Reason">camelCase end reason, e.g. <c>waterVictory</c>, <c>timeout</c>, <c>resign</c>.</param>
public sealed record MatchFinishedEto(
    Guid MatchId,
    string RulesVersion,
    Guid SouthPlayerId,
    Guid NorthPlayerId,
    string? Winner,
    string Reason,
    int Plies,
    long DurationMs,
    DateTime FinishedAt) : IEvent;

/// <summary>Published by Players when a player is banned (deactivated). Matches aborts their active games.</summary>
public sealed record PlayerBannedEto(Guid PlayerId) : IEvent;

/// <summary>Published by Players when a rating or display name changes. Matches keeps a read model for matchmaking.</summary>
public sealed record PlayerRatingChangedEto(Guid PlayerId, string DisplayName, double Rating, double RatingDeviation) : IEvent;
