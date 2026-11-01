namespace MittFmrte.Core.Models;

public sealed class Club : ObservableObject
{
    private long _transferBudget;
    private long _wageBudget;

    public required int Id { get; init; }
    public required string Name { get; init; }
    public string ShortName { get; init; } = "";
    public required string Country { get; init; }
    public required string League { get; init; }

    /// <summary>Club reputation 0-10000.</summary>
    public required int Reputation { get; init; }

    /// <summary>Whether the club normally refuses to sell its key players.</summary>
    public bool ReluctantSeller { get; init; }

    public long TransferBudget { get => _transferBudget; set => Set(ref _transferBudget, Math.Max(0, value)); }

    /// <summary>Highest weekly wage the club can offer a new signing.</summary>
    public long MaxWeeklyWage { get => _wageBudget; set => Set(ref _wageBudget, Math.Max(0, value)); }

    public override string ToString() => Name;
}
