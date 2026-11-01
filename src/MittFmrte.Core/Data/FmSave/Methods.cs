using System.Runtime.InteropServices;

namespace MittFmrte.Core.Data.FmSave;

/// <summary>Thin wrapper over ZstdSharp's frame-size helper.</summary>
internal static unsafe class Methods
{
    public static ulong ZSTD_findFrameCompressedSize(byte[] src, int offset, int length)
    {
        fixed (byte* p = &src[offset])
        {
            var r = ZstdSharp.Unsafe.Methods.ZSTD_findFrameCompressedSize(p, (nuint)length);
            return ZstdSharp.Unsafe.Methods.ZSTD_isError(r) ? 0 : r;
        }
    }
}
