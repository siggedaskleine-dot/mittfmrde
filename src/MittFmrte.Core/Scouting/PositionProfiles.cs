using MittFmrte.Core.Models;
using static MittFmrte.Core.Models.Attributes;

namespace MittFmrte.Core.Scouting;

/// <summary>A key attribute for a position and how much it matters (3 = vital, 2 = important, 1 = useful).</summary>
public readonly record struct KeyAttribute(string Name, int Weight);

/// <summary>
/// Which attributes matter for each position. Used both by the "Smarta värvningar" search
/// and by the "Toppa nyckelattribut" preset, so a player is judged on what his position needs
/// instead of on CA alone.
/// </summary>
public static class PositionProfiles
{
    private static readonly Dictionary<Position, KeyAttribute[]> Profiles = new()
    {
        [Position.Goalkeeper] =
        [
            new(Reflexes, 3), new(Handling, 3), new(OneOnOnes, 3), new(AerialReach, 2),
            new(CommandOfArea, 2), new(Positioning, 2), new(Concentration, 2), new(Kicking, 1), new(Communication, 1),
        ],
        [Position.CentreBack] =
        [
            new(Tackling, 3), new(Marking, 3), new(Heading, 3), new(Positioning, 3), new(JumpingReach, 2),
            new(Strength, 2), new(Concentration, 2), new(Anticipation, 2), new(Composure, 1), new(Pace, 1),
        ],
        [Position.FullBack] =
        [
            new(Pace, 3), new(Stamina, 3), new(Tackling, 2), new(Crossing, 2), new(WorkRate, 2),
            new(Acceleration, 2), new(Positioning, 2), new(Marking, 1), new(Teamwork, 1), new(Dribbling, 1),
        ],
        [Position.DefensiveMidfielder] =
        [
            new(Tackling, 3), new(Positioning, 3), new(Anticipation, 3), new(Passing, 2), new(WorkRate, 2),
            new(Teamwork, 2), new(Concentration, 2), new(Stamina, 2), new(Decisions, 1), new(Strength, 1),
        ],
        [Position.CentralMidfielder] =
        [
            new(Passing, 3), new(Decisions, 3), new(Vision, 2), new(FirstTouch, 2), new(Stamina, 2),
            new(WorkRate, 2), new(Teamwork, 2), new(Technique, 2), new(Composure, 1), new(Anticipation, 1),
        ],
        [Position.Winger] =
        [
            new(Pace, 3), new(Acceleration, 3), new(Dribbling, 3), new(Crossing, 2), new(Agility, 2),
            new(Technique, 2), new(OffTheBall, 2), new(Flair, 1), new(FirstTouch, 1), new(Stamina, 1),
        ],
        [Position.AttackingMidfielder] =
        [
            new(Vision, 3), new(Passing, 3), new(Technique, 3), new(FirstTouch, 2), new(Decisions, 2),
            new(Flair, 2), new(OffTheBall, 2), new(Composure, 2), new(Dribbling, 1), new(LongShots, 1),
        ],
        [Position.Striker] =
        [
            new(Finishing, 3), new(Composure, 3), new(OffTheBall, 3), new(Anticipation, 2), new(FirstTouch, 2),
            new(Pace, 2), new(Acceleration, 2), new(Dribbling, 1), new(Heading, 1), new(Strength, 1),
        ],
    };

    public static IReadOnlyList<KeyAttribute> For(Position position) => Profiles[position];

    /// <summary>
    /// How well a player fits a position, 0-100, as a weighted average of the key attributes
    /// (1 maps to 0 and 20 maps to 100).
    /// </summary>
    public static double Score(Player player, Position position)
    {
        var profile = For(position);
        double total = 0, weights = 0;
        foreach (var k in profile)
        {
            total += (player.AttrValue(k.Name) - 1) / 19.0 * k.Weight;
            weights += k.Weight;
        }
        return Math.Round(total / weights * 100, 1);
    }

    /// <summary>The player's weakest key attributes for a position, worst first.</summary>
    public static IEnumerable<(KeyAttribute Key, int Value)> Weakest(Player player, Position position, int count) =>
        For(position)
            .Select(k => (Key: k, Value: player.AttrValue(k.Name)))
            .OrderBy(x => x.Value)
            .ThenByDescending(x => x.Key.Weight)
            .Take(count);
}
