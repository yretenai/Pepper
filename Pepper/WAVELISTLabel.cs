using System;
using System.Runtime.InteropServices;
using Pepper.Structures;

namespace Pepper;

public class WAVELISTLabel {
	public static readonly WAVEChunkAtom Atom = "labl";

	public WAVELISTLabel(ReadOnlyMemory<byte> data) {
		Id = MemoryMarshal.Read<WAVEChunkAtom>(data.Span);
		Buffer = data[4..];
	}

	public WAVEChunkAtom Id { get; }
	public ReadOnlyMemory<byte> Buffer { get; }
}
