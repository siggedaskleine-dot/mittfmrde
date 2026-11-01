using MittFmrte.Core.Data;
using MittFmrte.Core.Models;
using MittFmrte.Core.Presets;
using MittFmrte.Core.Scouting;

namespace MittFmrte.Core.Tests;

public class SmartSigningTests
{
    private readonly GameWorld _world = new DemoGameDataSource().Load();

    [Theory]
    [InlineData(Position.Striker)]
    [InlineData(Position.CentreBack)]
    [InlineData(Position.Goalkeeper)]
    [InlineData(Position.Winger)]
    public void Results_are_realistic_and_upgrades(Position position)
    {
        var search = new SmartSigningSearch(_world);
        var club = _world.HumanClub;
        var best = search.CurrentBest(club.Id, position);
        var results = search.Find(new SigningOptions { Position = position, MaxAge = 40 });

        Assert.All(results, c =>
        {
            Assert.NotEqual(club.Id, c.Player.ClubId);
            Assert.Contains(position, c.Player.Positions);
            Assert.True(c.Player.Value <= club.TransferBudget);
            Assert.True(c.Player.Reputation <= club.Reputation * SmartSigningSearch.MaxReputationRatio);
            Assert.True(c.PositionScore > best);
            Assert.NotEmpty(c.Reasons);
        });
        Assert.Equal(results.OrderByDescending(r => r.PositionScore).Select(r => r.Player.Id), results.Select(r => r.Player.Id));
    }

    [Fact]
    public void Lower_bar_finds_candidates()
    {
        var results = new SmartSigningSearch(_world).Find(new SigningOptions { Position = Position.Striker, MaxAge = 40, MinScore = 0 });
        Assert.NotEmpty(results);
    }

    [Fact]
    public void Famous_players_are_rejected()
    {
        var search = new SmartSigningSearch(_world);
        var star = _world.Players.OrderByDescending(p => p.Reputation).First();
        var reason = search.Check(star, _world.HumanClub, new SigningOptions { Position = star.MainPosition, MaxAge = 40 }, 0);
        Assert.NotNull(reason);
    }

    [Fact]
    public void Position_score_weights_key_attributes()
    {
        var p = _world.Players.First(x => x.MainPosition == Position.Striker);
        foreach (var a in p.AttributeList) a.Value = 1;
        Assert.Equal(0, PositionProfiles.Score(p, Position.Striker));
        foreach (var k in PositionProfiles.For(Position.Striker)) p.Attr(k.Name)!.Value = 20;
        Assert.Equal(100, PositionProfiles.Score(p, Position.Striker));
    }
}

public class PresetTests
{
    private readonly GameWorld _world = new DemoGameDataSource().Load();

    private static IPlayerPreset Preset(string name) => PlayerPresets.All.First(p => p.Name == name);

    [Fact]
    public void Heal_and_undo()
    {
        var p = _world.Players.First();
        p.InjuryDays = 30;
        var log = new ChangeLog();
        log.Record(Preset("Läk").Apply(p, _world));
        Assert.Equal(0, p.InjuryDays);
        Assert.Equal(1, log.UndoLast());
        Assert.Equal(30, p.InjuryDays);
    }

    [Fact]
    public void Nothing_to_change_gives_no_changes()
    {
        var p = _world.Players.First();
        p.BanMatches = 0;
        Assert.Empty(Preset("Ta bort avstängning").Apply(p, _world));
    }

    [Fact]
    public void Renew_four_years_sets_end_of_season()
    {
        var p = _world.Players.First();
        Preset("Förläng 4 år").Apply(p, _world);
        Assert.Equal(new DateOnly(_world.GameDate.Year + 4, 6, 30), p.ContractExpires);
    }

    [Fact]
    public void Minus_one_year_never_goes_before_game_date()
    {
        var p = _world.Players.First();
        p.ContractExpires = _world.GameDate.AddMonths(3);
        Preset("-1 år kontrakt").Apply(p, _world);
        Assert.Equal(_world.GameDate, p.ContractExpires);
    }

    [Fact]
    public void Boost_raises_only_key_attributes_and_undoes()
    {
        var p = _world.Players.First(x => x.MainPosition == Position.Winger);
        foreach (var a in p.AttributeList) a.Value = 5;
        var log = new ChangeLog();
        log.Record(new BoostKeyAttributesPreset(15).Apply(p, _world));
        Assert.Equal(15, p.AttrValue(Attributes.Pace));
        Assert.Equal(5, p.AttrValue(Attributes.Heading));
        log.UndoLast();
        Assert.Equal(5, p.AttrValue(Attributes.Pace));
    }

    [Fact]
    public void Youth_talent_only_affects_young_players()
    {
        var old = _world.Players.First(x => x.AgeOn(_world.GameDate) > 25);
        Assert.Empty(Preset("Ungdomstalang").Apply(old, _world));
    }
}

public class SaveFileTests
{
    [Fact]
    public void Wrong_extension_is_rejected()
    {
        var f = Path.GetTempFileName();
        var ex = Assert.Throws<SaveFileException>(() => new SaveFileDataSource(f).Load());
        Assert.Contains(".fm", ex.Message);
    }
}

