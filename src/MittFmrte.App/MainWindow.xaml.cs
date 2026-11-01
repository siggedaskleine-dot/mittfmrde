using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using MittFmrte.App.Views;
using MittFmrte.Core.Data;
using MittFmrte.Core.Models;

namespace MittFmrte.App;

public partial class MainWindow : Window
{
    private readonly AppState _state = new();
    private readonly SquadView _squad;
    private readonly PlayerView _player;
    private readonly SigningsView _signings;

    public MainWindow()
    {
        InitializeComponent();
        _squad = new SquadView(_state);
        _player = new PlayerView(_state);
        _signings = new SigningsView(_state);

        _state.PlayerOpened += p => { _player.Show(p); Select("player"); };
        _state.ClubOpened += c => { _squad.ShowClub(c); Select("squad"); };
        _state.Status += msg =>
        {
            StatusLeft.Text = msg;
            UndoButton.IsEnabled = _state.Changes.CanUndo;
        };
    }

    // ------------------------------------------------------------- loading

    private async void LoadSave_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Välj en FM24-sparfil",
            Filter = "Football Manager-sparfil (*.fm)|*.fm|Alla filer (*.*)|*.*",
        };
        var games = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Sports Interactive", "Football Manager 2024", "games");
        if (Directory.Exists(games)) dialog.InitialDirectory = games;
        if (dialog.ShowDialog(this) != true) return;

        SetBusy(true);
        var progress = new Progress<string>(s => LoadingText.Text = s + "...");
        try
        {
            var path = dialog.FileName;
            var world = await Task.Run(() => new SaveFileDataSource(path, progress).Load());
            AgeConverter.GameDate = world.GameDate;
            _state.Load(world);
            Welcome.Visibility = Visibility.Collapsed;
            Nav.IsEnabled = SearchBox.IsEnabled = true;
            StatusRight.Text = $"{world.Source}  ·  Speldatum {world.GameDate:yyyy-MM-dd}  ·  {world.Players.Count:N0} spelare  ·  Ändringar sparas inte i filen än";
            _state.Say($"Laddade {world.Players.Count:N0} spelare. Din klubb: {world.HumanClub.Name}.");
            _squad.ShowClub(world.HumanClub);
            Select("squad");
        }
        catch (SaveFileException ex)
        {
            MessageBox.Show(this, ex.Message, "Kunde inte läsa sparfilen", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Något gick fel när sparfilen lästes:\n\n" + ex.Message, "Fel", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        LoadButton.IsEnabled = WelcomeLoadButton.IsEnabled = !busy;
        Cursor = busy ? Cursors.Wait : null;
        if (!busy) LoadingText.Text = "";
    }

    // ---------------------------------------------------------- navigation

    private void Select(string tag)
    {
        foreach (ListBoxItem item in Nav.Items)
            if ((string)item.Tag == tag) { Nav.SelectedItem = item; break; }
        ShowView(tag);
    }

    private void Nav_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Nav.SelectedItem is ListBoxItem item) ShowView((string)item.Tag);
    }

    private void ShowView(string tag)
    {
        var world = _state.World;
        switch (tag)
        {
            case "squad":
                Host.Content = _squad;
                SetHeader(_squad.Club?.Name ?? "", _squad.Club is { } c ? $"{_state.World!.SquadOf(c.Id).Count()} spelare" : "");
                break;
            case "player":
                Host.Content = _player;
                if (_player.Player is { } p)
                    SetHeader($"{p.FullName} ({p.AgeOn(world!.GameDate)} år)", _state.ClubName(p.ClubId));
                else
                    SetHeader("Spelare", "Dubbelklicka på en spelare i truppen eller sök efter en spelare");
                break;
            case "signings":
                Host.Content = _signings;
                SetHeader("Smarta värvningar", world is null ? "" : $"Realistiska förstärkningar för {world.HumanClub.Name}");
                break;
        }
    }

    private void SetHeader(string title, string subtitle)
    {
        HeaderTitle.Text = title;
        HeaderSubtitle.Text = subtitle;
    }

    // -------------------------------------------------------------- search

    private sealed record SearchHit(Player Player, string Name, string Club, string Ability);

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        SearchHint.Visibility = SearchBox.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        var text = SearchBox.Text.Trim();
        if (_state.World is null || text.Length < 3)
        {
            SearchPopup.IsOpen = false;
            return;
        }

        var hits = _state.World.Players
            .Where(p => p.FullName.Contains(text, StringComparison.CurrentCultureIgnoreCase)
                     || $"{p.FirstName} {p.LastName}".Contains(text, StringComparison.CurrentCultureIgnoreCase))
            .OrderByDescending(p => p.CurrentAbility)
            .Take(30)
            .Select(p => new SearchHit(p, p.FullName, _state.ClubName(p.ClubId), $"CA {p.CurrentAbility} / PA {p.PotentialAbility}"))
            .ToList();
        SearchResults.ItemsSource = hits;
        SearchPopup.IsOpen = hits.Count > 0;
    }

    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Down && SearchPopup.IsOpen)
        {
            SearchResults.SelectedIndex = 0;
            (SearchResults.ItemContainerGenerator.ContainerFromIndex(0) as UIElement)?.Focus();
        }
        else if (e.Key == Key.Enter && SearchResults.Items.Count > 0)
        {
            OpenHit((SearchHit)SearchResults.Items[0]);
        }
    }

    private void SearchResults_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (SearchResults.SelectedItem is SearchHit hit) OpenHit(hit);
    }

    private void SearchResults_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && SearchResults.SelectedItem is SearchHit hit) OpenHit(hit);
    }

    private void OpenHit(SearchHit hit)
    {
        SearchPopup.IsOpen = false;
        _state.OpenPlayer(hit.Player);
    }

    // ---------------------------------------------------------------- undo

    private void Undo_Click(object sender, RoutedEventArgs e)
    {
        var n = _state.Changes.UndoLast();
        _state.Say(n > 0 ? $"Ångrade {n} ändring(ar)." : "Inget att ångra.");
    }
}
