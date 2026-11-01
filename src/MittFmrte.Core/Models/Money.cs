using System.Globalization;

namespace MittFmrte.Core.Models;

public static class Money
{
    /// <summary>Formats like FM: €450K, €12.5M.</summary>
    public static string Format(long euros) => euros switch
    {
        >= 1_000_000 => "€" + (euros / 1_000_000d).ToString("0.#", CultureInfo.InvariantCulture) + "M",
        >= 1_000 => "€" + (euros / 1_000d).ToString("0", CultureInfo.InvariantCulture) + "K",
        _ => "€" + euros.ToString(CultureInfo.InvariantCulture),
    };
}
