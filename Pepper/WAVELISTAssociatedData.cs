using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Pepper.Structures;

namespace Pepper;

public class WAVELISTAssociatedData : WAVELIST, IChunkedFile {
	public new static readonly WAVEChunkAtom Atom = "adtl";

	public WAVELISTAssociatedData(IMemoryOwner<byte> storage, ReadOnlyMemory<byte> memory) : base(storage, memory) {
		var data = memory.Span;
		if (data.Length < 8) {
			throw new InvalidDataException("Insufficient data");
		}

		var cursor = 4;
		while (cursor < data.Length) {
			if (data.Length - cursor < Unsafe.SizeOf<WAVEChunkFragment>()) {
				break;
			}

			var fragment = MemoryMarshal.Read<WAVEChunkFragment>(data[cursor..]);
			Chunks.Add(cursor, fragment);
			cursor += Unsafe.SizeOf<WAVEChunkFragment>();

			if (fragment.Id == WAVELISTLabel.Atom) {
				try {
					Labels.Add(new WAVELISTLabel(memory.Slice(cursor, fragment.Size)));
				} catch (Exception e) {
					Debug.WriteLine($"failed parsing list label chunk: {e}", "pepper");
				}
			}

			cursor += fragment.Size;
		}
	}

	public List<WAVELISTLabel> Labels { get; set; } = [];

	public Dictionary<long, WAVEChunkFragment> Chunks { get; set; } = [];
}
