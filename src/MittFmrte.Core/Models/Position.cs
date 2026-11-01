namespace MittFmrte.Core.Models;

/// <summary>Simplified position groups. FM's detailed positions (D C, WB L, AM R ...) map onto these.</summary>
public enum Position
{
    Goalkeeper,
    CentreBack,
    FullBack,
    DefensiveMidfielder,
    CentralMidfielder,
    Winger,
    AttackingMidfielder,
    Striker,
}

public static class PositionNames
{
    public static string Swedish(Position p) => p switch
    {
        Position.Goalkeeper => "Målvakt",
        Position.CentreBack => "Mittback",
        Position.FullBack => "Ytterback",
        Position.DefensiveMidfielder => "Defensiv mittfältare",
        Position.CentralMidfielder => "Central mittfältare",
        Position.Winger => "Ytter",
        Position.AttackingMidfielder => "Offensiv mittfältare",
        Position.Striker => "Anfallare",
        _ => p.ToString(),
    };

    /// <summary>Short FM-style code shown in tables.</summary>
    public static string Short(Position p) => p switch
    {
        Position.Goalkeeper => "GK",
        Position.CentreBack => "D C",
        Position.FullBack => "D/WB",
        Position.DefensiveMidfielder => "DM",
        Position.CentralMidfielder => "M C",
        Position.Winger => "AM RL",
        Position.AttackingMidfielder => "AM C",
        Position.Striker => "ST",
        _ => p.ToString(),
    };
}

/// <summary>FM's own 15 detailed positions, in the order the save file stores their ratings.</summary>
public static class FmPositions
{
    public static readonly IReadOnlyList<string> Order =
        ["GK", "SW", "D L", "D C", "D R", "DM", "M L", "M C", "M R", "AM L", "AM C", "AM R", "ST", "WB L", "WB R"];

    private static readonly Position[] Groups =
    [
        Position.Goalkeeper, Position.CentreBack, Position.FullBack, Position.CentreBack, Position.FullBack,
        Position.DefensiveMidfielder, Position.Winger, Position.CentralMidfielder, Position.Winger,
        Position.Winger, Position.AttackingMidfielder, Position.Winger, Position.Striker,
        Position.FullBack, Position.FullBack,
    ];

    /// <summary>Rating at which FM shows a position as "natural"/accomplished.</summary>
    public const int Accomplished = 15;

    /// <summary>
    /// Simplified positions a player can play, best first. Always contains at least the best one.
    /// </summary>
    public static IReadOnlyList<Position> ToSimplified(IReadOnlyList<int> ratings)
    {
        var ranked = ratings
            .Select((r, i) => (Rating: r, Group: Groups[i]))
            .OrderByDescending(x => x.Rating)
            .ToList();
        var result = ranked.Where(x => x.Rating >= Accomplished).Select(x => x.Group).Distinct().ToList();
        if (result.Count == 0) result.Add(ranked[0].Group);
        return result;
    }

    /// <summary>FM-style text of the accomplished positions, e.g. "D C, D L".</summary>
    public static string Describe(IReadOnlyList<int> ratings) =>
        string.Join(", ", ratings
            .Select((r, i) => (r, i))
            .Where(x => x.r >= Accomplished)
            .OrderByDescending(x => x.r)
            .Select(x => Order[x.i]));
}
