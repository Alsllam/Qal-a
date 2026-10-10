using System.Globalization;

namespace Qala.Game.Matches.Domain.ValueObjects;

/// <summary>A clock setting: initial time per side plus an increment per move. Written <c>4+2</c> (minutes+seconds).</summary>
public sealed record TimeControl(int InitialSeconds, int IncrementSeconds)
{
    /// <summary>The provisional default: 4 minutes + 2 seconds.</summary>
    public static TimeControl Default { get; } = new(240, 2);

    public long InitialMs => InitialSeconds * 1000L;

    public long IncrementMs => IncrementSeconds * 1000L;

    public static bool TryParse(string? text, out TimeControl timeControl)
    {
        timeControl = Default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        var parts = text.Split('+');
        if (parts.Length != 2
            || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var minutes)
            || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var increment)
            || minutes is < 1 or > 60
            || increment is < 0 or > 60)
        {
            return false;
        }

        timeControl = new TimeControl(minutes * 60, increment);
        return true;
    }

    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{InitialSeconds / 60}+{IncrementSeconds}");
}
