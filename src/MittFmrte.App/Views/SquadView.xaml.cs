using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MittFmrte.Core.Models;
using MittFmrte.Core.Presets;

namespace MittFmrte.App.Views;

public partial class SquadView : UserControl
{
    private readonly AppState _state;
    private bool _suppressPicker;

    public Club? Club { get; private set; }

    public SquadView(AppState state)
    {
        InitializeComponent();
        _state = state;
        PresetButtons.ItemsSource = Presets.Available;
        _state.WorldLoaded += () =>
        {
            _suppressPicker = true;
            ClubPicker.ItemsSource = _state.World!.Clubs.Where(c => c.Id >= 0).OrderBy(c => c.Name).ToList();
            _suppressPicker = false;
        };
        UpdateTargetInfo();
    }

    public void ShowClub(Club club)
    {
        Club = club;
        _suppressPicker = true;
        ClubPicker.SelectedItem = club;
        _suppressPicker = false;
        var squad = _state.World!.SquadOf(club.Id).OrderByDescending(p => p.CurrentAbility).ToList();
        Grid.ItemsSource = squad;
        SquadInfo.Text = $"{squad.Count} spelare · Snitt-CA {(squad.Count == 0 ? 0 : squad.Average(p => p.CurrentAbility)):0}";
        UpdateTargetInfo();
    }

    private void ClubPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_suppressPicker && ClubPicker.SelectedItem is Club c) _state.OpenClub(c);
    }

    private IReadOnlyList<Player> Targets() =>
        ApplyToAll.IsChecked == true
            ? (Grid.ItemsSource as IEnumerable<Player>)?.ToList() ?? []
            : Grid.SelectedItems.Cast<Player>().ToList();

    private void UpdateTargetInfo()
    {
        var n = Targets().Count;
        TargetInfo.Text = n switch
        {
            0 => "Markera spelare i listan, eller kryssa i \"Gäller hela truppen\".",
            1 => "Gäller 1 spelare.",
            _ => $"Gäller {n} spelare.",
        };
    }

    private void Grid_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateTargetInfo();
    private void ApplyToAll_Changed(object sender, RoutedEventArgs e) => UpdateTargetInfo();

    private void Preset_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not IPlayerPreset preset || _state.World is null) return;
        var targets = Targets();
        if (targets.Count == 0)
        {
            _state.Say("Markera minst en spelare först.");
            return;
        }
        var changes = preset.ApplyAll(targets, _state.World);
        _state.Changes.Record(changes);
        Grid.Items.Refresh();
        _state.Say(changes.Count == 0
            ? $"{preset.Name}: inget behövde ändras."
            : $"{preset.Name}: {changes.Count} ändring(ar) på {targets.Count} spelare. (Sparas inte i filen än.)");
    }

    private void Grid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (Grid.SelectedItem is Player p) _state.OpenPlayer(p);
    }
}
