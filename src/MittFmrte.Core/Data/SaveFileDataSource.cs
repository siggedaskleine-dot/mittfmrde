using MittFmrte.Core.Data.FmSave;
using MittFmrte.Core.Models;

namespace MittFmrte.Core.Data;

/// <summary>Loads players and clubs from a Football Manager 2024 save (.fm). Read-only for now.</summary>
public sealed class SaveFileDataSource(string path, IProgress<string>? progress = null) : IGameDataSource
{
    public string Path { get; } = path;
    public string DisplayName => System.IO.Path.GetFileName(Path);

    public GameWorld Load()
    {
        var info = new FileInfo(Path);
        if (!info.Exists)
            throw new SaveFileException($"Hittar inte filen {Path}.");
        if (!string.Equals(info.Extension, ".fm", StringComparison.OrdinalIgnoreCase))
            throw new SaveFileException("Det här är inte en FM-sparfil. Välj en fil som slutar på .fm.");

        progress?.Report("Packar upp sparfilen");
        var data = FmSaveContainer.Decompress(File.ReadAllBytes(Path));
        var world = new FmSaveParser(data).Parse(DisplayName, System.IO.Path.GetFileNameWithoutExtension(Path), progress);
        if (world.Players.Count == 0)
            throw new SaveFileException("Sparfilen öppnades, men inga spelare hittades. Formatet kan skilja sig från FM24.");
        return world;
    }
}

public sealed class SaveFileException(string message) : Exception(message);
