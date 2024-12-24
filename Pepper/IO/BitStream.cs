using System;
using System.IO;
using Pepper.Structures;

namespace Pepper.IO;

internal sealed class BitStream {
	public BitStream(Memory<byte> data) => Data = data;

	public BitStream(Stream stream) => Stream = stream;

	private Memory<byte> Data { get; }
	private Stream? Stream { get; }
	private byte Current { get; set; }
	public byte BitsLeft { get; private set; }
	public int TotalBitsRead { get; private set; }
	public int TotalBytesRead { get; private set; }

	public bool GetBit() {
		if (BitsLeft == 0) {
			if (Stream != null) {
				TotalBitsRead++;
				Current = (byte) Stream.ReadByte();
			} else {
				Current = Data.Span[TotalBytesRead++];
			}

			BitsLeft = 8;
		}

		TotalBitsRead++;
		BitsLeft--;
		return (Current & (0x80 >> BitsLeft)) != 0;
	}

	public BitUint Read(byte bitSize) {
		var value = 0u;
		for (var i = 0; i < bitSize; i++) {
			if (GetBit()) {
				value |= 1U << i;
			}
		}

		return new BitUint(bitSize, value);
	}
}
