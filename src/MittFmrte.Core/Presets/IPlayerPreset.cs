using MittFmrte.Core.Models;

namespace MittFmrte.Core.Presets;

/// <summary>A one-click edit that can be applied to one or many players.</summary>
public interface IPlayerPreset
{
    string Name { get; }
    string Description { get; }

    /// <summary>Applies the preset and returns what changed (empty if nothing needed changing).</summary>
    IReadOnlyList<Change> Apply(Player player, GameWorld world);
}

public static class PresetExtensions
{
    public static IReadOnlyList<Change> ApplyAll(this IPlayerPreset preset, IEnumerable<Player> players, GameWorld world) =>
        players.SelectMany(p => preset.Apply(p, world)).ToList();
}
