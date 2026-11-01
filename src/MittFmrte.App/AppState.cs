using MittFmrte.Core.Models;
using MittFmrte.Core.Presets;

namespace MittFmrte.App;

/// <summary>Shared state for all views: the loaded game and the undo history.</summary>
public sealed class AppState
{
    public GameWorld? World { get; private set; }
    public ChangeLog Changes { get; } = new();

    public event Action? WorldLoaded;
    public event Action<Player>? PlayerOpened;
    public event Action<Club>? ClubOpened;
    public event Action<string>? Status;

    public void Load(GameWorld world)
    {
        World = world;
        while (Changes.CanUndo) Changes.UndoLast();
        WorldLoaded?.Invoke();
    }

    public void OpenPlayer(Player p) => PlayerOpened?.Invoke(p);
    public void OpenClub(Club c) => ClubOpened?.Invoke(c);
    public void Say(string message) => Status?.Invoke(message);

    public string ClubName(int clubId) => World?.Clubs.FirstOrDefault(c => c.Id == clubId)?.Name ?? "Klubblös";
}
