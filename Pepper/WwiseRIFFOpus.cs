using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Pepper.Structures;

namespace Pepper;

// ported from vgmstream
public sealed record WwiseRIFFOpus : WwiseRIFFFile {
	private static readonly byte[][] MappingMatrix = [[0], [0, 1], [0, 2, 1], [0, 1, 2, 3], [0, 4, 1, 2, 3], [0, 4, 1, 2, 3, 5], [0, 6, 1, 2, 3, 4, 5], [0, 6, 1, 2, 3, 4, 5, 7]];

	public WwiseRIFFOpus(Stream stream, bool forceStereo = false, bool leaveOpen = false) : base(stream, leaveOpen) {
		var tableChunk = Chunks.FirstOrDefault(x => x.Value.Id == 0x6b656573); // "seek"
		if (tableChunk.Value.Size == 0) {
			throw new InvalidDataException("Missing seek chunk");
		}

		if (FormatChunk.Codec is not WAVECodec.WwiseOpus) {
			throw new InvalidDataException("Not a WWise Opus file");
		}

		stream.Position = FormatOffset + 0x14;
		WAVEFormatChunkEx_WwiseOpus fmt = default;
		var buffer = MemoryMarshal.AsBytes(new Span<WAVEFormatChunkEx_WwiseOpus>(ref fmt));
		stream.ReadExactly(buffer);
		if ((fmt.ChannelLayout & 0xFF) == FormatChunk.Channels) {
			ChannelType = (byte) ((fmt.ChannelLayout >> 8) & 0x0F);
			fmt.ChannelLayout >>= 12;
		}

		FormatChunkEx = fmt;

		if (FormatChunkEx.MappingFamily == 1 && FormatChunk.Channels > 8) {
			throw new InvalidDataException("Too many channels for remapping");
		}

		if (FormatChunkEx.Version != 1) {
			throw new InvalidDataException("Invalid codec version");
		}

		ChannelMapping = new byte[FormatChunk.Channels];
		if (FormatChunkEx.MappingFamily > 0 && ChannelType == 1) {
			CoupledCount = (WAVEChannelMask) FormatChunkEx.ChannelLayout switch {
				               WAVEChannelMask.STEREO => 1,
				               WAVEChannelMask.TWOPOINT1 => 1,
				               WAVEChannelMask.QUAD_side => 2,
				               WAVEChannelMask.FIVEPOINT1 => 2,
				               WAVEChannelMask.SEVENPOINT1 => 2,
				               _ => 0,
			               };
			StreamCount = FormatChunk.Channels - CoupledCount;

			if (FormatChunkEx.MappingFamily == 1) {
				for (var i = 0; i < FormatChunk.Channels; i++) {
					ChannelMapping[i] = MappingMatrix[FormatChunk.Channels - 1][i];
				}
			} else {
				if (forceStereo && FormatChunk.Channels > 2) {
					StreamCount = FormatChunk.Channels;
					var fmt_ = FormatChunk;
					fmt_.Channels = 2;
					FormatChunk = fmt_;
					var fmtex_ = FormatChunkEx;
					fmtex_.MappingFamily = 1;
					FormatChunkEx = fmtex_;
				}

				if (FormatChunkEx.MappingFamily is 0 or 1 || (FormatChunkEx.ChannelLayout & 8) == 0) {
					for (var i = 0; i < FormatChunk.Channels; i++) {
						ChannelMapping[i] = (byte) i;
					}
				} else {
					for (var i = 0; i < FormatChunk.Channels; i++) {
						var idx = 0;
						for (var j = FormatChunkEx.ChannelLayout & 7; j > 0; j &= j - 1) {
							idx++;
						}

						if (idx == i) {
							ChannelMapping[i] = (byte) (idx - 1);
						} else if (i > idx) {
							ChannelMapping[i] = (byte) (i - 1);
						} else {
							ChannelMapping[i] = (byte) i;
						}
					}
				}
			}
		}

		if (FormatChunk.SampleRate == 0) {
			var fmt_ = FormatChunk;
			fmt_.SampleRate = 48000;
			FormatChunk = fmt_;
		}

		FrameTable = new ushort[FormatChunkEx.TableCount];
		var frameTable = FrameTable.AsSpan();
		stream.Position = tableChunk.Key;
		stream.ReadExactly(MemoryMarshal.AsBytes(frameTable));
	}

	private ushort[] FrameTable { get; }
	private byte ChannelType { get; }
	private int CoupledCount { get; }
	public int StreamCount { get; }
	private byte[] ChannelMapping { get; }

	public WAVEFormatChunkEx_WwiseOpus FormatChunkEx { get; }

	public override void Decode(Stream outputStream) {
		outputStream.SetLength(0);
		using var ogg = new BitOggStream(outputStream);
		Stream.Position = DataOffset;

	#region Opus Header

		Span<byte> buffer = stackalloc byte[21];
		buffer.Clear();
		"OpusHead"u8.CopyTo(buffer);
		buffer[8] = 1;
		buffer[9] = (byte) FormatChunk.Channels;
		// "pre-skip" is not the same as skip, apparently.
		// basically has no effect?
		// BinaryPrimitives.WriteInt16LittleEndian(buffer[10..], FormatChunkEx.Skip);
		BinaryPrimitives.WriteInt32LittleEndian(buffer[12..], FormatChunk.SampleRate);
		buffer[18] = FormatChunkEx.MappingFamily;
		buffer[19] = (byte) StreamCount;
		buffer[20] = (byte) CoupledCount;
		ogg.Write(buffer);
		ogg.Write(ChannelMapping);

		ogg.FlushPage();

	#endregion

	#region Opus Comment

		var vendor = Encoding.ASCII.GetBytes(Vendor);
		buffer = stackalloc byte[12];
		buffer.Clear();
		"OpusTags"u8.CopyTo(buffer);
		BinaryPrimitives.WriteInt32LittleEndian(buffer[8..], vendor.Length);
		ogg.Write(buffer);
		ogg.Write(vendor);
		ogg.PayloadBytes += 4; // User comment list length
		ogg.FlushPage();

	#endregion

		Stream.Position = DataOffset;
		var granule = 0;
		Span<byte> frame = stackalloc byte[0xFFFF];
		var skip = (int) FormatChunkEx.Skip;
		foreach (var frameSize in FrameTable) {
			Stream.ReadExactly(frame[..frameSize]);
			granule += GetNumberOfSamples(frame[..frameSize]) * GetSamplesPerFrame(frame[..frameSize], FormatChunk.SampleRate);
			if (skip > 0) {
				skip -= frameSize - 1;
				continue;
			}

			ogg.SetGranule(granule);
			ogg.Write(frame[..frameSize]);
			ogg.FlushPage(false, granule > FormatChunkEx.Samples);
		}
	}

	private static int GetSamplesPerFrame(Span<byte> data, int Fs) {
		int size;
		if ((data[0] & 0x80) != 0) {
			size = (data[0] >> 3) & 0x3;
			size = (Fs << size) / 400;
		} else if ((data[0] & 0x60) == 0x60) {
			size = (data[0] & 0x08) != 0 ? Fs / 50 : Fs / 100;
		} else {
			size = (data[0] >> 3) & 0x3;
			if (size == 3) {
				size = Fs * 60 / 1000;
			} else {
				size = (Fs << size) / 100;
			}
		}

		return size;
	}

	private static int GetNumberOfSamples(Span<byte> packet) {
		if (packet.Length < 1) {
			return 0;
		}

		var count = packet[0] & 0x3;
		if (count == 0) {
			return 1;
		}

		if (count != 3) {
			return 2;
		}

		if (packet.Length < 2) {
			return 0;
		}

		return packet[1] & 0x3F;
	}
}
