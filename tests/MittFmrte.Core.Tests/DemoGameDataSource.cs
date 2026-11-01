using MittFmrte.Core.Data;
using MittFmrte.Core.Models;
using MittFmrte.Core.Scouting;

namespace MittFmrte.Core.Tests;

/// <summary>
/// Fictional clubs and players for unit tests only. The app itself only shows data from a save file.
/// </summary>
public sealed class DemoGameDataSource(int seed = 2024) : IGameDataSource
{
    public string DisplayName => "Testdata";

    private static readonly (string Name, string Country, string League, int Rep, bool Reluctant)[] ClubDefs =
    [
        ("Nordhamn IF", "Sverige", "Allsvenskan", 5200, false),
        ("Västerås Kungar", "Sverige", "Allsvenskan", 4800, false),
        ("Malmvik FF", "Sverige", "Allsvenskan", 4500, false),
        ("Göta Hjärtan", "Sverige", "Allsvenskan", 5500, true),
        ("Lindholm BK", "Sverige", "Superettan", 3200, false),
        ("Ravnstad FC", "Danmark", "Superligaen", 5600, false),
        ("Fjordby SK", "Norge", "Eliteserien", 5000, false),
        ("Rotterburg", "Nederländerna", "Eredivisie", 6800, true),
        ("Atlético Serrano", "Spanien", "La Liga", 7400, true),
        ("Olympique Marais", "Frankrike", "Ligue 1", 8200, true),
        ("Borussia Weissfeld", "Tyskland", "Bundesliga", 7600, false),
        ("Calcio Venturi", "Italien", "Serie A", 7200, false),
        ("Ashford Rovers", "England", "Championship", 6000, false),
        ("Kingsbridge United", "England", "Premier League", 8600, true),
        ("Sporting Lagoa", "Portugal", "Liga Portugal", 6900, false),
        ("Dinamo Brasov", "Rumänien", "Liga 1", 4300, false),
    ];

    private static readonly (string Nation, string[] First, string[] Last)[] Names =
    [
        ("Sverige", ["Erik", "Oskar", "Liam", "Hugo", "Elias", "Nils", "Viktor", "Albin", "Måns", "Isak"],
                    ["Lindqvist", "Berg", "Holm", "Sjöberg", "Ek", "Nyström", "Falk", "Ström", "Dahl", "Wiklund"]),
        ("Danmark", ["Mads", "Rasmus", "Jonas", "Frederik", "Magnus"], ["Kjær", "Holst", "Lund", "Brandt", "Vestergaard"]),
        ("Norge", ["Sander", "Henrik", "Torbjørn", "Eirik"], ["Haugen", "Bakke", "Solberg", "Lie"]),
        ("Spanien", ["Pablo", "Diego", "Álvaro", "Iker", "Marcos"], ["Serrano", "Ibáñez", "Robles", "Cuesta", "Navarro"]),
        ("Frankrike", ["Lucas", "Théo", "Mathis", "Enzo", "Bastien"], ["Moreau", "Girard", "Lefèvre", "Roux", "Fournier"]),
        ("Tyskland", ["Jonas", "Felix", "Lukas", "Tim", "Niklas"], ["Weber", "Krüger", "Hoffmann", "Brandt", "Vogel"]),
        ("Brasilien", ["João", "Rafael", "Thiago", "Bruno", "Caio"], ["Silva", "Araújo", "Costa", "Moraes", "Pereira"]),
        ("England", ["Jack", "Harry", "Oliver", "George", "Callum"], ["Whitfield", "Hale", "Barrow", "Cooke", "Pennington"]),
        ("Nederländerna", ["Daan", "Sem", "Bram", "Thijs"], ["de Vries", "Bakker", "Visser", "Smit"]),
        ("Portugal", ["Tiago", "Rúben", "Gonçalo", "Nuno"], ["Ferreira", "Lopes", "Matos", "Teixeira"]),
    ];

    private static readonly Position[] SquadShape =
    [
        Position.Goalkeeper, Position.Goalkeeper, Position.Goalkeeper,
        Position.CentreBack, Position.CentreBack, Position.CentreBack, Position.CentreBack,
        Position.FullBack, Position.FullBack, Position.FullBack, Position.FullBack,
        Position.DefensiveMidfielder, Position.DefensiveMidfielder,
        Position.CentralMidfielder, Position.CentralMidfielder, Position.CentralMidfielder,
        Position.Winger, Position.Winger, Position.Winger, Position.Winger,
        Position.AttackingMidfielder, Position.AttackingMidfielder,
        Position.Striker, Position.Striker, Position.Striker,
    ];

    public GameWorld Load()
    {
        var rng = new Random(seed);
        var gameDate = new DateOnly(2023, 7, 10);
        var clubs = new List<Club>();
        var players = new List<Player>();
        var nextPlayerId = 1000;

        for (var i = 0; i < ClubDefs.Length; i++)
        {
            var d = ClubDefs[i];
            var club = new Club
            {
                Id = i + 1, Name = d.Name, Country = d.Country, League = d.League,
                Reputation = d.Rep, ReluctantSeller = d.Reluctant,
                TransferBudget = (long)(Math.Pow(d.Rep / 1000.0, 3.2) * 60_000),
                MaxWeeklyWage = (long)(Math.Pow(d.Rep / 1000.0, 2.6) * 600),
            };
            clubs.Add(club);

            var number = 1;
            foreach (var pos in SquadShape)
                players.Add(MakePlayer(rng, nextPlayerId++, club, pos, number++, gameDate));
        }

        return new GameWorld
        {
            Source = DisplayName,
            GameDate = gameDate,
            Clubs = clubs,
            Players = players,
            HumanClubId = 1,
        };
    }

    private static Player MakePlayer(Random rng, int id, Club club, Position pos, int number, DateOnly gameDate)
    {
        var names = rng.Next(100) < 55 && club.Country == "Sverige" ? Names[0] : Names[rng.Next(Names.Length)];
        var age = 17 + (int)Math.Round(Math.Abs(rng.NextDouble() + rng.NextDouble() - 0.6) * 9);
        var birth = gameDate.AddYears(-age).AddDays(-rng.Next(365));

        // Club strength decides the general level; key attributes for the position get a boost.
        var level = 4 + club.Reputation / 1000.0 + (rng.NextDouble() - 0.5) * 4;
        var keys = PositionProfiles.For(pos).ToDictionary(k => k.Name, k => k.Weight);

        var others = new List<Position>();
        if (rng.Next(100) < 30)
        {
            var alt = pos switch
            {
                Position.CentreBack => Position.DefensiveMidfielder,
                Position.FullBack => Position.Winger,
                Position.DefensiveMidfielder => Position.CentralMidfielder,
                Position.CentralMidfielder => Position.AttackingMidfielder,
                Position.Winger => Position.AttackingMidfielder,
                Position.AttackingMidfielder => Position.Winger,
                Position.Striker => Position.Winger,
                _ => pos,
            };
            if (alt != pos) others.Add(alt);
        }

        var player = new Player
        {
            Id = id,
            BirthDate = birth,
            Nationality = names.Nation,
            Positions = [pos, .. others],
            FirstName = names.First[rng.Next(names.First.Length)],
            LastName = names.Last[rng.Next(names.Last.Length)],
            ClubId = club.Id,
            SquadNumber = number,
        };

        void Add(IEnumerable<string> list, AttributeGroup group, bool isGkGroup)
        {
            foreach (var name in list)
            {
                double v = level + (rng.NextDouble() - 0.5) * 5;
                if (keys.TryGetValue(name, out var w)) v += w * 1.3;
                else if (isGkGroup != (pos == Position.Goalkeeper)) v -= 6;
                else v -= 1.5;
                if (age < 21) v -= (21 - age) * 0.6;
                player.AttributeList.Add(new PlayerAttribute(name, group, (int)Math.Round(v)));
            }
        }

        Add(Attributes.Technical, AttributeGroup.Technical, false);
        Add(Attributes.Mental, AttributeGroup.Mental, false);
        Add(Attributes.Physical, AttributeGroup.Physical, false);
        Add(Attributes.Goalkeeping, AttributeGroup.Goalkeeping, true);

        var score = PositionProfiles.Score(player, pos);
        player.CurrentAbility = (int)(30 + score * 1.45);
        var growth = age < 24 ? rng.Next(5, 50) : rng.Next(0, 8);
        player.PotentialAbility = Math.Max(player.CurrentAbility, player.CurrentAbility + growth);
        player.Reputation = (int)Math.Clamp(club.Reputation * (0.55 + player.CurrentAbility / 300.0) + rng.Next(-600, 600), 100, 9800);

        var ageFactor = age switch { <= 21 => 1.4, <= 26 => 1.2, <= 29 => 1.0, <= 31 => 0.6, _ => 0.3 };
        player.Value = (long)(Math.Pow(player.CurrentAbility / 20.0, 4.2) * 160 * ageFactor) / 5000 * 5000;
        player.WeeklyWage = Math.Max(500, (long)(player.Value / 450.0) / 100 * 100);
        player.ContractExpires = new DateOnly(gameDate.Year + rng.Next(1, 5), 6, 30);
        player.Morale = rng.Next(40, 95);
        player.InjuryDays = rng.Next(100) < 8 ? rng.Next(5, 60) : 0;
        player.BanMatches = rng.Next(100) < 4 ? rng.Next(1, 3) : 0;
        return player;
    }
}
