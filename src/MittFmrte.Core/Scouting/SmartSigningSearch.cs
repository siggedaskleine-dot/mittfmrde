using MittFmrte.Core.Models;

namespace MittFmrte.Core.Scouting;

public sealed record SigningOptions
{
    public required Position Position { get; init; }
    public int MaxAge { get; init; } = 30;
    public int MaxResults { get; init; } = 25;

    /// <summary>Only show players at least this good for the position (0-100). Defaults to "better than your current best".</summary>
    public double? MinScore { get; init; }
}

public sealed record SigningCandidate(
    Player Player,
    Club CurrentClub,
    int Age,
    double PositionScore,
    double Upgrade,
    IReadOnlyList<string> Reasons);

/// <summary>Why a player was left out. Exposed so the UI and tests can explain the filtering.</summary>
public enum RejectReason { OwnPlayer, WrongPosition, TooOld, TooFamous, TooExpensive, WageTooHigh, ClubWontSell, NotGoodEnough }

/// <summary>
/// "Smarta värvningar": finds players who are strong in the key attributes of a position
/// and who are realistic for the human club to sign. Read-only: it never changes data.
/// </summary>
public sealed class SmartSigningSearch(GameWorld world)
{
    /// <summary>A player won't move to a club whose reputation is much lower than his own.</summary>
    public const double MaxReputationRatio = 1.15;

    /// <summary>Reluctant sellers only let go of players who aren't among their best.</summary>
    public const int ReluctantSellerKeepTop = 11;

    public IReadOnlyList<SigningCandidate> Find(SigningOptions options)
    {
        var club = world.HumanClub;
        var currentBest = CurrentBest(club.Id, options.Position);
        var minScore = options.MinScore ?? currentBest;

        var result = new List<SigningCandidate>();
        foreach (var player in world.Players)
        {
            if (Check(player, club, options, minScore) is not null) continue;

            var score = PositionProfiles.Score(player, options.Position);
            var age = player.AgeOn(world.GameDate);
            result.Add(new SigningCandidate(
                player, world.ClubById(player.ClubId), age, score,
                Math.Round(score - currentBest, 1),
                Explain(player, club, score, currentBest, age)));
        }

        return result
            .OrderByDescending(c => c.PositionScore)
            .ThenBy(c => c.Age)
            .ThenBy(c => c.Player.Value)
            .Take(options.MaxResults)
            .ToList();
    }

    /// <summary>Returns why a player is not a realistic signing, or null if he is.</summary>
    public RejectReason? Check(Player player, Club buyer, SigningOptions options, double minScore)
    {
        if (player.ClubId == buyer.Id) return RejectReason.OwnPlayer;
        if (!player.Positions.Contains(options.Position)) return RejectReason.WrongPosition;
        if (player.AgeOn(world.GameDate) > options.MaxAge) return RejectReason.TooOld;
        if (player.Reputation > buyer.Reputation * MaxReputationRatio) return RejectReason.TooFamous;
        if (player.Value > buyer.TransferBudget) return RejectReason.TooExpensive;
        if (player.WeeklyWage is { } wage && wage * 1.1 > buyer.MaxWeeklyWage) return RejectReason.WageTooHigh;
        if (IsProtectedBySeller(player)) return RejectReason.ClubWontSell;
        if (PositionProfiles.Score(player, options.Position) <= minScore) return RejectReason.NotGoodEnough;
        return null;
    }

    /// <summary>Best position score among the human club's own players for this position (0 if none).</summary>
    public double CurrentBest(int clubId, Position position) =>
        world.SquadOf(clubId)
            .Where(p => p.Positions.Contains(position))
            .Select(p => PositionProfiles.Score(p, position))
            .DefaultIfEmpty(0)
            .Max();

    private bool IsProtectedBySeller(Player player)
    {
        var seller = world.ClubById(player.ClubId);
        if (!seller.ReluctantSeller) return false;
        var rank = world.SquadOf(seller.Id).Count(p => p.CurrentAbility > player.CurrentAbility);
        return rank < ReluctantSellerKeepTop;
    }

    private static IReadOnlyList<string> Explain(Player p, Club buyer, double score, double currentBest, int age)
    {
        var reasons = new List<string>
        {
            currentBest > 0
                ? $"{score - currentBest:+0.0;-0.0} mot din bästa på positionen"
                : "Du saknar spelare på positionen",
            $"Kostar {Money.Format(p.Value)} av {Money.Format(buyer.TransferBudget)} i budget",
            "Rykte passar din klubb",
        };
        if (p.WeeklyWage is { } wage)
            reasons.Add($"Lön {Money.Format(wage)}/v (max {Money.Format(buyer.MaxWeeklyWage)})");
        if (age <= 21 && p.PotentialAbility - p.CurrentAbility >= 20)
            reasons.Add("Ung med stor potential");
        return reasons;
    }
}
