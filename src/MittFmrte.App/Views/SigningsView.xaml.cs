using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MittFmrte.Core.Models;
using MittFmrte.Core.Scouting;

namespace MittFmrte.App.Views;

public partial class SigningsView : UserControl
{
    private readonly AppState _state;

    public sealed record PositionItem(Position Position, string Name);

    public sealed record Row(Player Player, Club CurrentClub, int Age, double PositionScore, double Upgrade, string ReasonText);

    public SigningsView(AppState state)
    {
        InitializeComponent();
        _state = state;
        PositionPicker.ItemsSource = Enum.GetValues<Position>().Select(p => new PositionItem(p, PositionNames.Swedish(p))).ToList();
        PositionPicker.SelectedIndex = 7; // Anfallare
        _state.WorldLoaded += () =>
        {
            Budget.Text = _state.World!.HumanClub.TransferBudget.ToString("N0", CultureInfo.CurrentCulture);
            Results.ItemsSource = null;
        };
    }

    private void Search_Click(object sender, RoutedEventArgs e)
    {
        var world = _state.World;
        if (world is null || PositionPicker.SelectedValue is not Position position) return;

        if (!int.TryParse(MaxAge.Text, out var maxAge)) maxAge = 30;
        var digits = new string(Budget.Text.Where(char.IsDigit).ToArray());
        if (long.TryParse(digits, out var budget)) world.HumanClub.TransferBudget = budget;

        var search = new SmartSigningSearch(world);
        var best = search.CurrentBest(world.HumanClubId, position);
        var options = new SigningOptions
        {
            Position = position,
            MaxAge = maxAge,
            MaxResults = 50,
            MinScore = OnlyUpgrades.IsChecked == true ? null : 0,
        };
        var results = search.Find(options)
            .Select(c => new Row(c.Player, c.CurrentClub, c.Age, c.PositionScore, c.Upgrade, string.Join(" · ", c.Reasons)))
            .ToList();
        Results.ItemsSource = results;

        var posName = PositionNames.Swedish(position).ToLowerInvariant();
        Summary.Text = results.Count == 0
            ? $"Hittade ingen realistisk {posName} som är bättre än din bästa (betyg {best:0}). Höj budgeten eller avmarkera \"Bara spelare bättre än min bästa\"."
            : $"{results.Count} realistiska alternativ som {posName}. Din bästa just nu har betyg {best:0}/100. Dubbelklicka för att öppna en spelare.";
    }

    private void Results_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (Results.SelectedItem is Row r) _state.OpenPlayer(r.Player);
    }
}
