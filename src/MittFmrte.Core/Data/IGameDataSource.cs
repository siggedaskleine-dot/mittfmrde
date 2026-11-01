using MittFmrte.Core.Models;

namespace MittFmrte.Core.Data;

/// <summary>
/// Where game data comes from. Step 1 only has <see cref="MockGameDataSource"/>;
/// step 2 adds a source that reads a running FM24, step 4 possibly one for .fm save files.
/// </summary>
public interface IGameDataSource
{
    string DisplayName { get; }
    GameWorld Load();
}
