using MittFmrte.Core.Presets;

namespace MittFmrte.App;

/// <summary>
/// Presets the app offers. Ones that need data the save reader can't read yet
/// (injuries, bans, morale, contracts) are left out until that data is decoded.
/// </summary>
public static class Presets
{
    private static readonly HashSet<string> NeedsUndecodedData =
        ["Läk", "Inspirera", "Ta bort avstängning", "Förläng 4 år", "+1 år kontrakt", "-1 år kontrakt"];

    public static IReadOnlyList<IPlayerPreset> Available { get; } =
        PlayerPresets.All.Where(p => !NeedsUndecodedData.Contains(p.Name)).ToList();
}
