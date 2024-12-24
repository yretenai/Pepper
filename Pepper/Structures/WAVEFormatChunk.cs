using System.Runtime.InteropServices;

namespace Pepper.Structures;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct WAVEFormatChunk {
	public static readonly WAVEChunkAtom Atom = "fmt ";
	public WAVECodec Codec { get; set; }
	public ushort Channels { get; set; }
	public int SampleRate { get; set; }
	public int ByteRate { get; set; }
	public ushort BlockSize { get; set; }
	public ushort BitRate { get; set; }
}
