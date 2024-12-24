using System;
using System.Buffers;
using System.IO;
using System.Runtime.InteropServices;
using Pepper.Structures;

namespace Pepper;

public class WAVELIST : IDisposable {
	public static readonly WAVEChunkAtom Atom = "LIST";

	public WAVELIST(IMemoryOwner<byte> storage, ReadOnlyMemory<byte> memory) {
		var data = memory.Span;
		if (data.Length < 4) {
			throw new InvalidDataException("Insufficient data");
		}

		Id = MemoryMarshal.Read<WAVEChunkAtom>(data);
		Data = storage;
	}

	public WAVEChunkAtom Id { get; }
	public IMemoryOwner<byte> Data { get; }

	public void Dispose() {
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	public static WAVELIST? FromBytes(IMemoryOwner<byte> storage, int fragmentSize) {
		if (fragmentSize < 4) {
			return null;
		}

		var data = storage.Memory[..fragmentSize];
		var id = MemoryMarshal.Read<WAVEChunkAtom>(data.Span);
		if (id == WAVELISTAssociatedData.Atom) {
			return new WAVELISTAssociatedData(storage, data);
		}

		return new WAVELIST(storage, data);
	}

	protected virtual void Dispose(bool disposing) {
		if (disposing) {
			Data.Dispose();
		}
	}
}
