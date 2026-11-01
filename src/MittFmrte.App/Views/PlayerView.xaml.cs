using System.Windows;
using System.Windows.Controls;
using MittFmrte.Core.Models;
using MittFmrte.Core.Presets;
using MittFmrte.Core.Scouting;

namespace MittFmrte.App.Views;

public partial class PlayerView : UserControl
{
    private readonly AppState _state;

    public Player? Player { get; private set; }

    public sealed record AttrRow(PlayerAttribute Attr, bool IsKey);
    public sealed record PositionRow(string Name, int Rating);

    public PlayerView(AppState state)
    {
        InitializeComponent();
        _state = state;
        PresetButtons.ItemsSource = Presets.Available;
        _state.WorldLoaded += () => { Player = null; Body.Visibility = Visibility.Collapsed; Empty.Visibility = Visibility.Visible; };
    }

    public void Show(Player player)
    {
        if (Player is not null)
            foreach (var a in Player.AttributeList) a.PropertyChanged -= Attribute_Changed;

        Player = player;
        DataContext = player;
        Empty.Visibility = Visibility.Collapsed;
        Body.Visibility = Visibility.Visible;

        var keys = PositionProfiles.For(player.MainPosition).Select(k => k.Name).ToHashSet();
        List<AttrRow> Rows(AttributeGroup g) =>
            player.AttributeList.Where(a => a.Group == g).Select(a => new AttrRow(a, keys.Contains(a.Name))).ToList();

        TechnicalList.ItemsSource = Rows(AttributeGroup.Technical);
        MentalList.ItemsSource = Rows(AttributeGroup.Mental);
        PhysicalList.ItemsSource = Rows(AttributeGroup.Physical);
        GoalkeepingList.ItemsSource = Rows(AttributeGroup.Goalkeeping);
        HiddenList.ItemsSource = Rows(AttributeGroup.Feet).Concat(Rows(AttributeGroup.Hidden)).ToList();

        PositionList.ItemsSource = player.PositionRatings
            .Select((r, i) => new PositionRow(FmPositions.Order[i], r))
            .Where(p => p.Rating > 1)
            .OrderByDescending(p => p.Rating)
            .ToList();

        FillInfo(player);
        foreach (var a in player.AttributeList) a.PropertyChanged += Attribute_Changed;
        UpdateFit();
    }

    private void FillInfo(Player p)
    {
        InfoGrid.Children.Clear();
        InfoGrid.RowDefinitions.Clear();
        var world = _state.World!;
        (string Label, string Value)[] rows =
        [
            ("Förnamn", p.FirstName),
            ("Efternamn", p.LastName),
            ("Smeknamn", p.CommonName ?? "–"),
            ("Fullständigt namn", p.FullLegalName ?? "–"),
            ("Född", $"{p.BirthDate:yyyy-MM-dd} ({p.AgeOn(world.GameDate)} år)"),
            ("Klubb", _state.ClubName(p.ClubId)),
            ("Positioner", p.PositionText),
            ("Värde", MittFmrte.Core.Models.Money.Format(p.Value)),
        ];
        for (var i = 0; i < rows.Length; i++)
        {
            InfoGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var label = new TextBlock { Text = rows[i].Label, Margin = new Thickness(0, 2, 12, 2), Foreground = (System.Windows.Media.Brush)FindResource("TextDim") };
            var value = new TextBlock { Text = rows[i].Value, Margin = new Thickness(0, 2, 0, 2), TextWrapping = TextWrapping.Wrap };
            Grid.SetRow(label, i);
            Grid.SetRow(value, i);
            Grid.SetColumn(value, 1);
            InfoGrid.Children.Add(label);
            InfoGrid.Children.Add(value);
        }
    }

    private void Attribute_Changed(object? sender, System.ComponentModel.PropertyChangedEventArgs e) => UpdateFit();

    private void UpdateFit()
    {
        if (Player is null) return;
        var pos = Player.MainPosition;
        var score = PositionProfiles.Score(Player, pos);
        FitScore.Text = $"{score:0}/100";
        var weakest = PositionProfiles.Weakest(Player, pos, 3).Select(w => $"{w.Key.Name} ({w.Value})");
        FitText.Text = $"Som {PositionNames.Swedish(pos).ToLowerInvariant()}. Svagast: {string.Join(", ", weakest)}.";
    }

    private void Preset_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not IPlayerPreset preset || Player is null || _state.World is null) return;
        var changes = preset.Apply(Player, _state.World);
        _state.Changes.Record(changes);
        _state.Say(changes.Count == 0 ? $"{preset.Name}: inget behövde ändras." : $"{preset.Name}: {changes.Count} ändring(ar). (Sparas inte i filen än.)");
        UpdateFit();
    }
}
