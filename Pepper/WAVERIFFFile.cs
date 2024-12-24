using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Pepper.Structures;

namespace Pepper;

public class WAVERIFFFile : IDisposable, IChunkedFile {
	public WAVERIFFFile(Stream stream, bool leaveOpen = false) {
		Stream = stream;
		LeaveOpen = leaveOpen;

		FileStart = stream.Position;
		Span<uint> header = stackalloc uint[3];
		var buffer = MemoryMarshal.AsBytes(header);
		stream.ReadExactly(buffer);
		if (header[0] is not 0x46464952 || header[2] is not 0x45564157) { // "RIFF", "WAVE"
			throw new InvalidDataException("Not a WAVE-RIFF file");
		}

		FileSize = (int) header[1] + 8;

		WAVEChunkFragment fragment = default;
		var fragmentSpan = new Span<WAVEChunkFragment>(ref fragment);
		while (stream.Position < FileStart + FileSize) {
			stream.ReadExactly(MemoryMarshal.AsBytes(fragmentSpan));
			Chunks.Add(stream.Position, fragment);

			if (fragment.Id == WAVEFormatChunk.Atom) {
				FormatOffset = stream.Position;
			} else if (fragment.Id == WAVEChunkAtom.DataAtom) {
				DataOffset = stream.Position;
			} else if (fragment.Id == WAVELIST.Atom) {
				var chunkBytes = MemoryPool<byte>.Shared.Rent(fragment.Size);
				stream.ReadExactly(chunkBytes.Memory.Span[.. fragment.Size]);

				try {
					var listChunk = WAVELIST.FromBytes(chunkBytes, fragment.Size);
					if (listChunk != null) {
						ListChunks.Add(listChunk);
					}
				} catch (Exception e) {
					Debug.WriteLine($"failed parsing list chunk: {e}", "pepper");
					chunkBytes.Dispose();
				}

				continue; // skip size increment
			}

			stream.Position += fragment.Size;
		}

		var fmtChunk = Chunks[FormatOffset];
		if (fmtChunk.Size < 0x10) {
			throw new InvalidDataException("Invalid fmt size");
		}

		stream.Position = FormatOffset;
		WAVEFormatChunk fmt = default;
		var fmtSpan = new Span<WAVEFormatChunk>(ref fmt);
		stream.ReadExactly(MemoryMarshal.AsBytes(fmtSpan));
		FormatChunk = fmt;
		buffer = stackalloc byte[2];
		if (fmtChunk.Size >= 0x12) {
			stream.ReadExactly(buffer);
			FormatExtraSize = BinaryPrimitives.ReadUInt16LittleEndian(buffer) - 2;
		}
	}

	public string Vendor { get; set; } = "Pepper";

	public virtual AudioFormat Format => AudioFormat.Ogg;
	protected Stream Stream { get; set; }
	protected int FormatExtraSize { get; }
	private bool LeaveOpen { get; }
	public WAVEFormatChunk FormatChunk { get; protected set; }
	public int FileSize { get; }
	public long FileStart { get; }
	protected long FormatOffset { get; }
	protected long DataOffset { get; }
	public List<WAVELIST> ListChunks { get; set; } = [];
	public Dictionary<long, WAVEChunkFragment> Chunks { get; set; } = [];

	public void Dispose() {
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	~WAVERIFFFile() {
		Dispose(false);
	}

	protected virtual void Dispose(bool disposing) {
		if (disposing) {
			if (!LeaveOpen) {
				Stream.Dispose();
			}

			foreach (var chunk in ListChunks) {
				chunk.Dispose();
			}
		}
	}

	public virtual void Decode(Stream outputStream) {
		Stream.Position = 0;
		Stream.CopyTo(outputStream);
	}

	public override string ToString() => $"WAVE {{ {Chunks.Count} chunks, {FormatChunk} }}";

	public bool TryFindNameLabel([MaybeNullWhen(false)] out string label) {
		foreach (var listChunk in ListChunks) {
			if (listChunk is WAVELISTAssociatedData associatedData) {
				foreach (var labelChunk in associatedData.Labels) {
					if (labelChunk.Id == ProMExportLabel.Atom) {
						var prom = new ProMExportLabel(labelChunk);
						if (!string.IsNullOrEmpty(prom.FriendlyName)) {
							label = prom.FriendlyName;
							return true;
						}
					} else if (labelChunk.Id == default) {
						if (labelChunk.Buffer.Span.ContainsAnyExceptInRange<byte>(0x20, 0x7A)) {
							continue;
						}

						label = Encoding.ASCII.GetString(labelChunk.Buffer.Span);
					}
				}
			}
		}

		label = null;
		return false;
	}
}
