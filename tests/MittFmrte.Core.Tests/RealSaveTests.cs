using MittFmrte.Core.Data;
using MittFmrte.Core.Models;
using Xunit.Abstractions;

namespace MittFmrte.Core.Tests;

/// <summary>
/// Runs against a real FM24 save when MITTFMRTE_SAVE points at one. Expected values come from
/// FMRTE screenshots of the save "Mattias Nordström - Paris SG.fm" (game date 2023-07-10).
/// </summary>
public class RealSaveTests(ITestOutputHelper output)
{
    private static readonly string? SavePath = Environment.GetEnvironmentVariable("MITTFMRTE_SAVE");

    private static readonly Lazy<GameWorld> World = new(() => new SaveFileDataSource(SavePath!).Load());

    [SkippableFact]
    public void Reads_psg_squad_like_fmrte()
    {
        Skip.If(string.IsNullOrEmpty(SavePath) || !File.Exists(SavePath), "MITTFMRTE_SAVE not set");
        var w = World.Value;
        output.WriteLine($"{w.Players.Count} players, {w.Clubs.Count} clubs, date {w.GameDate}, human {w.HumanClub.Name}");

        var unnamed = w.Clubs.Count(c => c.Name.StartsWith("Klubb #"));
        output.WriteLine($"{unnamed} clubs without a name");
        foreach (var c in w.Clubs.Where(c => c.Name.StartsWith("Klubb #")).Take(6))
            output.WriteLine($"  {c.Name}: {string.Join(", ", w.SquadOf(c.Id).Take(4).Select(p => p.FullName))}");
        foreach (var who in new[] { "Kevin De Bruyne", "Toni Kroos", "Robert Lewandowski", "Lautaro Martínez", "Thomas Müller", "Dušan Vlahović", "Neymar", "Santiago Giménez" })
        {
            var p = w.Players.FirstOrDefault(x => x.FullName == who);
            output.WriteLine($"  {who}: {(p is null ? "?" : w.Clubs.FirstOrDefault(c => c.Id == p.ClubId)?.Name)}");
        }
        var search = new MittFmrte.Core.Scouting.SmartSigningSearch(w);
        output.WriteLine($"Budget estimate {w.HumanClub.TransferBudget:N0}, club rep {w.HumanClub.Reputation}");
        foreach (var pos in new[] { Position.Striker, Position.CentreBack, Position.Winger })
        {
            var r = search.Find(new MittFmrte.Core.Scouting.SigningOptions { Position = pos, MaxAge = 30, MaxResults = 5 });
            output.WriteLine($"{pos} best {search.CurrentBest(w.HumanClubId, pos):0}: " + string.Join("; ", r.Select(c => $"{c.Player.FullName} {c.PositionScore:0} {c.CurrentClub.Name} {Money.Format(c.Player.Value)}")));
        }
        Assert.InRange(w.Players.Count, 12000, 13000);
        Assert.Equal(new DateOnly(2023, 7, 10), w.GameDate);
        Assert.Equal("Paris Saint-Germain", w.HumanClub.Name);

        var squad = w.SquadOf(w.HumanClubId).ToDictionary(p => p.FullName);
        foreach (var p in squad.Values.OrderBy(p => p.CurrentAbility))
            output.WriteLine($"{p.FullName,-28} {p.PositionText,-18} {p.CurrentAbility}/{p.PotentialAbility} {p.BirthDate}");

        (string Name, int Ca, int Pa)[] expected =
        [
            ("Keylor Navas", 151, 167), ("Gianluigi Donnarumma", 161, 170), ("Achraf Hakimi", 161, 164),
            ("Layvin Kurzawa", 125, 140), ("Bradley Barcola", 143, 166), ("Marco Asensio", 159, 163),
            ("Presnel Kimpembe", 150, 162), ("Beraldo", 129, 158), ("Nordi Mukiele", 142, 150),
        ];
        foreach (var (name, ca, pa) in expected)
        {
            Assert.True(squad.ContainsKey(name), $"{name} missing from PSG squad");
            Assert.Equal(ca, squad[name].CurrentAbility);
            Assert.Equal(pa, squad[name].PotentialAbility);
        }

        var kimpembe = squad["Presnel Kimpembe"];
        Assert.Equal(new DateOnly(1995, 8, 13), kimpembe.BirthDate);
        Assert.Equal(20, kimpembe.AttrValue(Attributes.LeftFoot));
        Assert.Equal(17, kimpembe.AttrValue(Attributes.Tackling));
        Assert.Equal(Position.CentreBack, kimpembe.MainPosition);
        Assert.Equal(19, squad["Achraf Hakimi"].AttrValue(Attributes.Pace));
        Assert.Equal(Position.Goalkeeper, squad["Keylor Navas"].MainPosition);
    }
}

