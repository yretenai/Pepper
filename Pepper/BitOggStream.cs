using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using Pepper.Structures;

namespace Pepper;

internal sealed class BitOggStream(Stream stream) : IDisposable {
	private const int PageBufferSize = (int) SizeEnum.HeaderBytes + (int) SizeEnum.MaxSegments + (int) SizeEnum.SegmentSize * (int) SizeEnum.MaxSegments;
	private IMemoryOwner<byte> PageBufferRaw { get; } = MemoryPool<byte>.Shared.Rent(PageBufferSize);

	private Stream Output { get; } = stream;

	private byte BitBuffer { get; set; }
	private uint BitsStored { get; set; }
	public int PayloadBytes { get; set; }
	private bool First { get; set; } = true;
	private bool Continue { get; set; }
	private uint Granule { get; set; }
	private uint SEQN { get; set; }
	private Span<byte> PageBuffer => PageBufferRaw.Memory.Span[..PageBufferSize];

	public void Dispose() {
		FlushPage();
		PageBufferRaw.Dispose();
	}

	public void PutBit(bool bit) {
		if (bit) {
			BitBuffer |= (byte) (1 << (byte) BitsStored);
		}

		BitsStored++;
		if (BitsStored == 8) {
			FlushBits();
		}
	}

	public void SetGranule(uint g) {
		Granule = g;
	}

	public void FlushBits() {
		if (BitsStored == 0) {
			return;
		}

		if (PayloadBytes == (int) SizeEnum.SegmentSize * (int) SizeEnum.MaxSegments) {
			throw new Exception("ran out of space in an Ogg packet");
		}

		PageBuffer[(int) SizeEnum.HeaderBytes + (int) SizeEnum.MaxSegments + PayloadBytes] = BitBuffer;
		PayloadBytes++;

		BitsStored = 0;
		BitBuffer = 0;
	}

	public void FlushPage(bool nextContinued = false, bool last = false) {
		if (PayloadBytes != (int) SizeEnum.SegmentSize * (int) SizeEnum.MaxSegments) {
			FlushBits();
		}

		if (PayloadBytes != 0) {
			var segments = (PayloadBytes + (int) SizeEnum.SegmentSize) / (int) SizeEnum.SegmentSize; // intentionally round up
			if (segments == (int) SizeEnum.MaxSegments + 1) {
				segments = (int) SizeEnum.MaxSegments; // at max eschews the final 0
			}

			// move payload back
			for (var i = 0; i < PayloadBytes; i++) {
				PageBuffer[(int) SizeEnum.HeaderBytes + segments + i] = PageBuffer[(int) SizeEnum.HeaderBytes + (int) SizeEnum.MaxSegments + i];
			}

			BinaryPrimitives.WriteUInt32LittleEndian(PageBuffer, 0x5367674f); // "OggS"
			PageBuffer[4] = 0; // stream_structure_version
			PageBuffer[5] = (byte) ((Continue ? 1 : 0) | (First ? 2 : 0) | (last ? 4 : 0)); // header_type_flag

			BinaryPrimitives.WriteUInt64LittleEndian(PageBuffer[6..], Granule); // granule sample
			BinaryPrimitives.WriteUInt32LittleEndian(PageBuffer[14..], 1); // stream serial number
			BinaryPrimitives.WriteUInt32LittleEndian(PageBuffer[18..], SEQN); // page sequence number
			BinaryPrimitives.WriteUInt32LittleEndian(PageBuffer[22..], 0); // checksum (0 for now)
			PageBuffer[26] = (byte) segments; // segment count

			// lacing values
			for (int i = 0, bytesLeft = PayloadBytes; i < segments; i++) {
				if (bytesLeft >= (int) SizeEnum.SegmentSize) {
					bytesLeft -= (int) SizeEnum.SegmentSize;
					PageBuffer[27 + i] = (int) SizeEnum.SegmentSize;
				} else {
					PageBuffer[27 + i] = (byte) bytesLeft;
				}
			}

			// checksum
			BinaryPrimitives.WriteUInt32LittleEndian(PageBuffer[22..], Checksum.Compute(PageBuffer, (int) SizeEnum.HeaderBytes + segments + PayloadBytes));

			// output to ostream
			Output.Write(PageBuffer[..((int) SizeEnum.HeaderBytes + segments + PayloadBytes)]);

			SEQN++;
			First = false;
			Continue = nextContinued;
			PayloadBytes = 0;
			PageBuffer.Clear();
		}
	}

	public void Write(BitUint bui) {
		for (var i = 0; i < bui.BitSize; i++) {
			PutBit((bui.Value & (1U << i)) != 0);
		}
	}

	public void Write(ReadOnlySpan<byte> bytes) {
		FlushBits();
		bytes.CopyTo(PageBuffer.Slice((int) SizeEnum.HeaderBytes + (int) SizeEnum.MaxSegments + PayloadBytes, bytes.Length));
		PayloadBytes += bytes.Length;
	}

	private enum SizeEnum {
		HeaderBytes = 27,
		MaxSegments = 255,
		SegmentSize = 255,
	}
}
