using ZstdSharp;

namespace MittFmrte.Core.Data.FmSave;

/// <summary>
/// The outer layer of an FM24 .fm file: a small "fmf" header followed by many zstd frames.
/// Decompressing the frames in order gives one long byte array that holds the whole game.
/// </summary>
public static class FmSaveContainer
{
    private static readonly byte[] FmfTag = "fmf"u8.ToArray();
    private static readonly byte[] ZstdMagic = [0x28, 0xB5, 0x2F, 0xFD];

    public static bool LooksLikeFmSave(ReadOnlySpan<byte> file) =>
        file.Length > 32 && file.Slice(2, 3).SequenceEqual(FmfTag) && file.IndexOf(ZstdMagic) is >= 0 and < 4096;

    /// <summary>Decompresses every zstd frame in the file, in order.</summary>
    public static byte[] Decompress(byte[] file, IProgress<double>? progress = null)
    {
        if (!LooksLikeFmSave(file))
            throw new SaveFileException("Filen ser inte ut som en Football Manager-sparfil.");

        using var output = new MemoryStream(capacity: file.Length * 5);
        var pos = file.AsSpan().IndexOf(ZstdMagic);
        var frames = 0;

        while (pos >= 0 && pos < file.Length)
        {
            var compressedSize = FrameSize(file, pos);
            if (compressedSize <= 0)
            {
                // Not a real frame start (the magic bytes can occur by chance); look further.
                pos = NextMagic(file, pos + 1);
                continue;
            }

            using (var decompressor = new Decompressor())
            {
                var chunk = decompressor.Unwrap(file.AsSpan(pos, compressedSize));
                output.Write(chunk);
            }
            frames++;
            pos += compressedSize;
            if (!file.AsSpan(pos).StartsWith(ZstdMagic))
                pos = NextMagic(file, pos);
            progress?.Report((double)pos / file.Length);
        }

        if (frames == 0)
            throw new SaveFileException("Hittade ingen komprimerad data i sparfilen.");
        return output.ToArray();
    }

    private static int NextMagic(byte[] file, int from)
    {
        if (from >= file.Length) return -1;
        var i = file.AsSpan(from).IndexOf(ZstdMagic);
        return i < 0 ? -1 : from + i;
    }

    /// <summary>Size in bytes of the zstd frame starting at <paramref name="pos"/>, or -1 if it isn't valid.</summary>
    private static int FrameSize(byte[] file, int pos)
    {
        try
        {
            var size = Methods.ZSTD_findFrameCompressedSize(file, pos, file.Length - pos);
            return size > 0 && size <= (ulong)(file.Length - pos) ? (int)size : -1;
        }
        catch
        {
            return -1;
        }
    }
}
