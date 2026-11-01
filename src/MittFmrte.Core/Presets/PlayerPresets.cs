using MittFmrte.Core.Models;
using MittFmrte.Core.Scouting;

namespace MittFmrte.Core.Presets;

/// <summary>Generic preset that sets one value and remembers the old one.</summary>
internal sealed class ValuePreset<T>(
    string name, string description,
    Func<Player, T> get, Action<Player, T> set,
    Func<Player, GameWorld, T> target, Func<T, string> show) : IPlayerPreset
{
    public string Name => name;
    public string Description => description;

    public IReadOnlyList<Change> Apply(Player player, GameWorld world)
    {
        var old = get(player);
        var value = target(player, world);
        if (EqualityComparer<T>.Default.Equals(old, value)) return [];
        set(player, value);
        return [new Change($"{player.FullName}: {name} ({show(old)} → {show(value)})", () => set(player, old))];
    }
}

/// <summary>Raises a player's weak key attributes for his main position to a target level.</summary>
public sealed class BoostKeyAttributesPreset(int target = 15) : IPlayerPreset
{
    public string Name => "Toppa nyckelattribut";
    public string Description => $"Höjer de viktigaste attributen för spelarens position till minst {target}.";

    public IReadOnlyList<Change> Apply(Player player, GameWorld world)
    {
        var changes = new List<Change>();
        foreach (var key in PositionProfiles.For(player.MainPosition))
        {
            var attr = player.Attr(key.Name);
            if (attr is null || attr.Value >= target) continue;
            var old = attr.Value;
            attr.Value = target;
            changes.Add(new Change($"{player.FullName}: {key.Name} {old} → {target}", () => attr.Value = old));
        }
        return changes;
    }
}

/// <summary>The built-in presets, in the order they are shown.</summary>
public static class PlayerPresets
{
    private static DateOnly EndOfSeason(DateOnly from, int years) => new(from.Year + years, 6, 30);

    private static string Date(DateOnly? d) => d?.ToString("yyyy-MM-dd") ?? "okänt";

    public static IReadOnlyList<IPlayerPreset> All { get; } =
    [
        new ValuePreset<int>("Läk", "Tar bort skador.",
            p => p.InjuryDays, (p, v) => p.InjuryDays = v, (_, _) => 0, v => $"{v} dagar"),
        new ValuePreset<int>("Inspirera", "Sätter moralen till max.",
            p => p.Morale, (p, v) => p.Morale = v, (_, _) => 100, v => v.ToString()),
        new ValuePreset<int>("Ta bort avstängning", "Tar bort avstängningar.",
            p => p.BanMatches, (p, v) => p.BanMatches = v, (_, _) => 0, v => $"{v} matcher"),
        new ValuePreset<DateOnly?>("Förläng 4 år", "Kontraktet gäller till sommaren om fyra år.",
            p => p.ContractExpires, (p, v) => p.ContractExpires = v,
            (p, w) => p.ContractExpires is null ? null : EndOfSeason(w.GameDate, 4), Date),
        new ValuePreset<DateOnly?>("+1 år kontrakt", "Förlänger kontraktet ett år.",
            p => p.ContractExpires, (p, v) => p.ContractExpires = v, (p, _) => p.ContractExpires?.AddYears(1), Date),
        new ValuePreset<DateOnly?>("-1 år kontrakt", "Kortar kontraktet ett år (aldrig före dagens datum).",
            p => p.ContractExpires, (p, v) => p.ContractExpires = v,
            (p, w) => p.ContractExpires is not { } c ? null : c.AddYears(-1) < w.GameDate ? w.GameDate : c.AddYears(-1), Date),
        new BoostKeyAttributesPreset(),
        new ValuePreset<int>("Ungdomstalang", "Spelare 21 år eller yngre får PA +20 (max 200).",
            p => p.PotentialAbility, (p, v) => p.PotentialAbility = v,
            (p, w) => p.AgeOn(w.GameDate) <= 21 ? Math.Min(200, p.PotentialAbility + 20) : p.PotentialAbility,
            v => v.ToString()),
    ];
}
