namespace MittFmrte.Core.Models;

/// <summary>Everything the editor knows about one loaded game.</summary>
public sealed class GameWorld
{
    public required string Source { get; init; }
    public required DateOnly GameDate { get; init; }
    public required IReadOnlyList<Club> Clubs { get; init; }
    public required IReadOnlyList<Player> Players { get; init; }

    /// <summary>The club the human manager controls.</summary>
    public required int HumanClubId { get; init; }

    public Club HumanClub => ClubById(HumanClubId);

    public Club ClubById(int id) => Clubs.First(c => c.Id == id);

    public IEnumerable<Player> SquadOf(int clubId) => Players.Where(p => p.ClubId == clubId);
}
