using System.Runtime.InteropServices;

namespace Pepper.Structures;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct WAVESampleChunk {
    public int Count { get; set; }
    public int Start { get; set; }
    public int End { get; set; }
}
