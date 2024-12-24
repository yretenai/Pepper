using System.Runtime.InteropServices;

namespace Pepper.Structures;

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 12)]
public record struct AKBKHeader {
	public static readonly WAVEChunkAtom Atom = "BKHD";

	public uint Version { get; set; }
	public uint Id { get; set; }
	public uint LanguageId { get; set; }
}
