using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using OggVorbisSharp;
using Pepper.IO;
using Pepper.Structures;
using static OggVorbisSharp.Ogg;
using static OggVorbisSharp.Vorbis;

namespace Pepper;

// ported from ww2ogg
public class WwiseRIFFVorbis : WaveRIFFFile {
	public WwiseRIFFVorbis(Stream stream, string codebookPath, bool leaveOpen = false) : base(stream, leaveOpen) {
		using var codebooksStream = new FileStream(codebookPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		WwiseCodebooks = new WwiseCodebook(codebooksStream);

		var smplChunk = Chunks.FirstOrDefault(x => x.Key == 0x6c706d73); // smpl
		var vorbChunk = Chunks.FirstOrDefault(x => x.Key == 0x62726f76); // smpl

		if (smplChunk.Key > 0) {
			stream.Position = smplChunk.Key;
			WAVESampleChunk loop = default;
			var loopBuffer = MemoryMarshal.AsBytes(new Span<WAVESampleChunk>(ref loop));
			stream.ReadExactly(loopBuffer);
			Loop = loop;
		}

		if (vorbChunk.Key == 0) {
			vorbChunk = new KeyValuePair<long, WAVEChunkFragment>(FormatOffset + 0x18, new WAVEChunkFragment {
				Id = 0x62726f76,
				Size = 0,
			});
		}

		stream.Position = vorbChunk.Key;
		var temp = 0u;
		var intBuffer = MemoryMarshal.AsBytes(new Span<uint>(ref temp));
		stream.ReadExactly(intBuffer);
		SampleCount = (int) temp;

		if (vorbChunk.Value.Size is 0 or 0x2A) {
			NoGranule = true;

			stream.Position = vorbChunk.Key + 0x4;
			stream.ReadExactly(intBuffer);
			ModSignal = (int) temp;
			if ((ModSignal & 0x70) != 1) {
				ModPackets = true;
			}

			stream.Position = vorbChunk.Key + 0x10;
		} else {
			stream.Position = vorbChunk.Key + 0x18;
		}

		stream.ReadExactly(intBuffer);
		SetupPacketOffset = (int) temp;
		stream.ReadExactly(intBuffer);
		FirstAudioPacketOffset = (int) temp;

		stream.Position += 12;

		if (vorbChunk.Value.Size is 0x28 or 0x2C) {
			HeaderTriadPresent = true;
			OldPacketHeaders = true;
		} else {
			stream.Position += 4; // uid
			BlockSize0 = (byte) stream.ReadByte();
			BlockSize1 = (byte) stream.ReadByte();
		}

		if (Loop.End == 0) {
			var loop = Loop;
			loop.End = SampleCount;
			Loop = loop;
		} else if (Loop.Start > Loop.End) {
			var loop = Loop;
			(loop.Start, loop.End) = (loop.End, loop.Start);
			Loop = loop;
		}
	}

	public WAVESampleChunk Loop { get; set; }
	public int SampleCount { get; }
	public bool NoGranule { get; }
	public bool ModPackets { get; }
	public int ModSignal { get; }
	public int SetupPacketOffset { get; }
	public int FirstAudioPacketOffset { get; }
	private bool HeaderTriadPresent { get; }
	private bool OldPacketHeaders { get; }
	private byte BlockSize0 { get; }
	private byte BlockSize1 { get; }
	public WwiseCodebook WwiseCodebooks { get; }

	public override void Decode(Stream outputStream) {
		var tmpStream = WemHelper.CanUseRevorb ? new MemoryStream() : outputStream;
		tmpStream.SetLength(0);
		try {
			using var os = new BitOggStream(tmpStream);

			var prevBlockflag = false;
			if (HeaderTriadPresent) {
				throw new NotSupportedException();
			}

			GenerateOggHeader(os, out var modeBlockflag, out var modeBits);

			var dataSize = Chunks[DataOffset].Size;
			// audio pages
			{
				var offset = DataOffset + FirstAudioPacketOffset;

				while (offset < DataOffset + dataSize) {
					uint size;
					uint granule;
					long packetHeaderSize;
					long packetPayloadOffset;
					long nextOffset;

					if (OldPacketHeaders) {
						throw new NotSupportedException();
					}

					{
						var audioPacket = new VorbisPacket(Stream, offset, NoGranule);
						packetHeaderSize = audioPacket.HeaderSize();
						size = audioPacket.Size;
						packetPayloadOffset = audioPacket.Offset();
						granule = audioPacket.Granule;
						nextOffset = audioPacket.NextOffset();
					}

					offset = packetPayloadOffset;

					Stream.Position = packetPayloadOffset;

					os.SetGranule(granule == 0xFFFFFFFF ? -1 : (int) granule);

					if (ModPackets) {
						if (modeBlockflag == null) {
							throw new Exception("didn't load blockflag");
						}

						os.Write(new BitUint(1));

						BitUint modeNumberP;
						BitUint remainderP;

						{
							// collect mode number from first byte
							var inBits = new BitStream(Stream);

							// IN/OUT: N bit mode number (max 6 bits)
							modeNumberP = inBits.Read((byte) modeBits);
							os.Write(modeNumberP);

							// IN: remaining bits of first (input) byte
							remainderP = inBits.Read((byte) (8 - modeBits));
						}

						if (modeBlockflag[modeNumberP]) {
							// long window, peek at next frame
							Stream.Position = nextOffset;
							var nextBlockflag = false;
							if (nextOffset + packetHeaderSize <= DataOffset + dataSize) {
								// mod_packets always goes with 6-byte headers
								var audioPacket = new VorbisPacket(Stream, nextOffset, NoGranule);
								uint nextPacketSize = audioPacket.Size;

								if (nextPacketSize > 0) {
									Stream.Position = audioPacket.Offset();

									var inBits = new BitStream(Stream);
									var nextModeNumber = inBits.Read((byte) modeBits);
									nextBlockflag = modeBlockflag[nextModeNumber];
								}
							}

							os.Write(new BitUint(1, (uint) (prevBlockflag ? 1 : 0)));

							os.Write(new BitUint(1, (uint) (nextBlockflag ? 1 : 0)));

							Stream.Position = offset + 1;
						}

						prevBlockflag = modeBlockflag[modeNumberP];

						os.Write(remainderP);
					} else {
						// nothing unusual for first byte
						var v = Stream.ReadByte();
						if (v < 0) {
							throw new Exception("file truncated");
						}

						os.Write(new BitUint(8, (uint) v));
					}

					// remainder of packet
					for (uint i = 1; i < size; i++) {
						var v = Stream.ReadByte();
						if (v < 0) {
							throw new Exception("file truncated");
						}

						os.Write(new BitUint(8, (uint) v));
					}

					offset = nextOffset;
					os.FlushPage(false, offset == DataOffset + dataSize);
				}

				if (offset > DataOffset + dataSize) {
					throw new Exception("page truncated");
				}
			}

			if (WemHelper.CanUseRevorb) {
				try {
					tmpStream.Position = 0;
					outputStream.Position = 0;
					outputStream.SetLength(0);
					Rebuild(outputStream, tmpStream);
				} catch {
					tmpStream.Position = 0;
					outputStream.Position = 0;
					outputStream.SetLength(0);
					outputStream.CopyTo(tmpStream);
				}
			}
		} finally {
			if (WemHelper.CanUseRevorb) {
				tmpStream.Dispose();
			}
		}
	}

	private static void WriteVorbisPacketHeader(byte type, Span<byte> data) {
		data[0] = type;
		"vorbis"u8.CopyTo(data[1..]);
	}

	private void GenerateOggHeader(BitOggStream os, out bool[] modeBlockflag, out int modeBits) {
	#region Header

		{
			Span<byte> stack = stackalloc byte[28];
			WriteVorbisPacketHeader(1, stack);
			// BinaryPrimitives.WriteUInt32LittleEndian(stack[7..], 0); // version
			stack[11] = (byte) FormatChunk.Channels;
			BinaryPrimitives.WriteInt32LittleEndian(stack[12..], FormatChunk.SampleRate);
			// BinaryPrimitives.WriteUInt32LittleEndian(stack[16..], 0); // bitrate max
			BinaryPrimitives.WriteUInt32LittleEndian(stack[20..], (uint) FormatChunk.ByteRate * 8);
			// BinaryPrimitives.WriteUInt32LittleEndian(stack[24..], 0); // bitrate min
			os.Write(stack);
			os.Write(new BitUint(4, BlockSize0));
			os.Write(new BitUint(4, BlockSize1));
			os.Write(new BitUint(1, 1));
			os.FlushPage();
		}

	#endregion

	#region Comment

		{
			Span<byte> stack = stackalloc byte[11];
			WriteVorbisPacketHeader(3, stack);

			var vendor = Encoding.ASCII.GetBytes(Vendor);

			BinaryPrimitives.WriteInt32LittleEndian(stack[7..], vendor.Length);
			os.Write(stack);
			os.Write(vendor);

			if (Loop.Count == 0) {
				os.Write(new BitUint(32));
			} else {
				var comments = new List<string> {
					$"LoopStart={Loop.Start}",
					$"LoopEnd={Loop.End}",
				};

				os.Write(new BitUint(32, (uint) comments.Count));

				foreach (var comment in comments) {
					os.Write(new BitUint(32, (uint) comment.Length));
					os.Write(Encoding.ASCII.GetBytes(comment));
				}
			}

			os.Write(new BitUint(1, 1));
			os.FlushPage();
		}

	#endregion

	#region Codebook

		{
			Span<byte> stack = stackalloc byte[7];
			WriteVorbisPacketHeader(5, stack);
			os.Write(stack);

			var setupVorbisPacket = new VorbisPacket(Stream, DataOffset + SetupPacketOffset, NoGranule);

			Stream.Position = setupVorbisPacket.Offset();
			if (setupVorbisPacket.Granule != 0) {
				throw new Exception("setup packet granule != 0");
			}

			var bitStream = new BitStream(Stream);

			var codebookCountLess1 = bitStream.Read(8);

			var codebookCount = codebookCountLess1 + 1;
			os.Write(codebookCountLess1);

			for (var i = 0; i < codebookCount; i++) {
				var codebookID = bitStream.Read(10);
				WwiseCodebooks.Rebuild(codebookID.AsInt(), os);
			}

			os.Write(new BitUint(6));
			os.Write(new BitUint(16));

			{
				// floor count
				var floorCountLess1 = bitStream.Read(6);
				var floorCount = floorCountLess1 + 1;
				os.Write(floorCountLess1);

				// rebuild floors
				for (uint i = 0; i < floorCount; i++) {
					os.Write(new BitUint(16, 1));

					var floor1Partitions = bitStream.Read(5);
					os.Write(floor1Partitions);

					var floor1PartitionClassList = new uint[floor1Partitions];

					uint maximumClass = 0;
					for (var j = 0; j < floor1Partitions; j++) {
						var floor1PartitionClass = bitStream.Read(4);
						os.Write(floor1PartitionClass);

						floor1PartitionClassList[j] = floor1PartitionClass;

						if (floor1PartitionClass > maximumClass) {
							maximumClass = floor1PartitionClass;
						}
					}

					var floor1ClassDimensionsList = new uint[maximumClass + 1];

					for (var j = 0; j <= maximumClass; j++) {
						var classDimensionsLess1 = bitStream.Read(3);
						os.Write(classDimensionsLess1);

						floor1ClassDimensionsList[j] = classDimensionsLess1 + 1;

						var classSubclasses = bitStream.Read(2);
						os.Write(classSubclasses);

						if (classSubclasses != 0) {
							var masterBook = bitStream.Read(8);
							os.Write(masterBook);

							if (masterBook >= codebookCount) {
								throw new Exception("invalid floor1 masterbook");
							}
						}

						for (uint k = 0; k < 1U << classSubclasses.AsInt(); k++) {
							var subclassBookPlus1 = bitStream.Read(8);
							os.Write(subclassBookPlus1);

							var subclassBook = subclassBookPlus1.AsInt() - 1;
							if (subclassBook >= 0 && subclassBook >= codebookCount) {
								throw new Exception("invalid floor1 subclass book");
							}
						}
					}

					var floor1MultiplierLess1 = bitStream.Read(2);
					os.Write(floor1MultiplierLess1);

					var rangebits = bitStream.Read(4);
					os.Write(rangebits);

					for (uint j = 0; j < floor1Partitions; j++) {
						var currentClassNumber = floor1PartitionClassList[j];
						for (uint k = 0; k < floor1ClassDimensionsList[currentClassNumber]; k++) {
							var x = bitStream.Read((byte) rangebits);
							os.Write(x);
						}
					}
				}

				// residue count
				var residueCountLess1 = bitStream.Read(6);
				var residueCount = residueCountLess1 + 1;
				os.Write(residueCountLess1);

				for (uint i = 0; i < residueCount; i++) {
					var residueType = bitStream.Read(2);
					os.Write(new BitUint(16, residueType));

					if (residueType > 2) {
						throw new Exception("invalid residue type");
					}

					var residueBegin = bitStream.Read(24);
					var residueEnd = bitStream.Read(24);
					var residuePartitionSizeLess1 = bitStream.Read(24);
					var residueClassificationsLess1 = bitStream.Read(6);
					var residueClassbook = bitStream.Read(8);
					var residueClassifications = residueClassificationsLess1 + 1;
					os.Write(residueBegin);
					os.Write(residueEnd);
					os.Write(residuePartitionSizeLess1);
					os.Write(residueClassificationsLess1);
					os.Write(residueClassbook);

					if (residueClassbook >= codebookCount) {
						throw new Exception("invalid residue classbook");
					}

					var residueCascade = new uint[residueClassifications];
					for (uint j = 0; j < residueClassifications; j++) {
						var highBits = new BitUint(5);
						var lowBits = bitStream.Read(3);
						os.Write(lowBits);

						var bitFlag = bitStream.Read(1);
						os.Write(bitFlag);
						if (bitFlag == 1) {
							bitStream.Read(5);
							os.Write(highBits);
						}

						residueCascade[j] = highBits * 8 + lowBits;
					}

					for (uint j = 0; j < residueClassifications; j++) {
						for (var k = 0; k < 8; k++) {
							if ((residueCascade[j] & (1 << k)) != 0) {
								var residueBook = bitStream.Read(8);
								os.Write(residueBook);

								if (residueBook >= codebookCount) {
									throw new Exception("invalid residue book");
								}
							}
						}
					}
				}

				var mappingCountLess1 = bitStream.Read(6);
				var mappingCount = mappingCountLess1 + 1;
				os.Write(mappingCountLess1);

				for (uint i = 0; i < mappingCount; i++) {
					os.Write(new BitUint(16));

					var submapsFlag = bitStream.Read(1);
					os.Write(submapsFlag);

					uint submaps = 1;
					if (submapsFlag == 1) {
						var submapsLess1 = bitStream.Read(4);
						submaps = submapsLess1 + 1;
						os.Write(submapsLess1);
					}

					var squarePolarFlag = bitStream.Read(1);
					os.Write(squarePolarFlag);

					if (squarePolarFlag == 1) {
						var couplingStepsLess1 = bitStream.Read(8);
						var couplingSteps = couplingStepsLess1 + 1;
						os.Write(couplingStepsLess1);

						for (uint j = 0; j < couplingSteps; j++) {
							var magnitude = bitStream.Read((byte) (32 - BitOperations.LeadingZeroCount((uint) (FormatChunk.Channels - 1))));
							var angle = bitStream.Read((byte) (32 - BitOperations.LeadingZeroCount((uint) (FormatChunk.Channels - 1))));

							os.Write(magnitude);
							os.Write(angle);

							if (angle == magnitude || magnitude >= FormatChunk.Channels || angle >= FormatChunk.Channels) {
								throw new Exception("invalid coupling");
							}
						}
					}

					// a rare reserved field not removed by Ak!
					var mappingReserved = bitStream.Read(2);
					os.Write(mappingReserved);
					if (0 != mappingReserved) {
						throw new Exception("mapping reserved field nonzero");
					}

					if (submaps > 1) {
						for (uint j = 0; j < FormatChunk.Channels; j++) {
							var mappingMux = bitStream.Read(4);
							os.Write(mappingMux);

							if (mappingMux >= submaps) {
								throw new Exception("mapping_mux >= submaps");
							}
						}
					}

					for (uint j = 0; j < submaps; j++) {
						var timeConfig = bitStream.Read(8);
						os.Write(timeConfig);

						var floorNumber = bitStream.Read(8);
						os.Write(floorNumber);

						if (floorNumber >= floorCount) {
							throw new Exception("invalid floor mapping");
						}

						var residueNumber = bitStream.Read(8);
						os.Write(residueNumber);

						if (residueNumber >= residueCount) {
							throw new Exception("invalid residue mapping");
						}
					}
				}

				// mode count
				var modeCountLess1 = bitStream.Read(6);
				var modeCount = modeCountLess1 + 1;
				os.Write(modeCountLess1);

				modeBlockflag = new bool[modeCount];
				modeBits = (byte) (32 - BitOperations.LeadingZeroCount(modeCount - 1));

				for (uint i = 0; i < modeCount; i++) {
					var blockFlag = bitStream.Read(1);
					os.Write(blockFlag);

					modeBlockflag[i] = blockFlag != 0;

					// only 0 valid for windowtype and transformtype
					os.Write(new BitUint(16));
					os.Write(new BitUint(16));

					var mapping = bitStream.Read(8);
					os.Write(mapping);
					if (mapping >= mappingCount) {
						throw new Exception("invalid mode mapping");
					}
				}
			}

			os.Write(new BitUint(1, 1));
			os.FlushPage();
		}

	#endregion
	}


	// ported from librevorb/revorb
	// note: provide libogg.dll and libvorbis.dll on windows, rely on system libs for unix
	[SuppressMessage("Performance", "CA1806:Do not ignore method results")]
	public static unsafe void Rebuild(Stream outputStream, Stream inputStream) {
		ogg_sync_state sync_in = default;
		ogg_sync_state sync_out = default;
		ogg_stream_state stream_in = default;
		ogg_stream_state stream_out = default;
		vorbis_info vi = default;

		try {
			ogg_sync_init(&sync_in);
			ogg_sync_init(&sync_out);
			vorbis_info_init(&vi);

			ogg_packet packet = default;
			ogg_page page = default;

			if (!CopyHeaders(inputStream, &sync_in, &stream_in, outputStream, &sync_out, &stream_out, &vi)) {
				throw new InvalidOperationException();
			}

			var granpos = 0L;
			var packetnum = 0L;
			var lastbs = 0L;

			ogg_page opage = default;
			while (true) {
				var eos = false;
				while (!eos) {
					var res = ogg_sync_pageout(&sync_in, &page);
					if (res < 0) {
						throw new InvalidOperationException();
					}

					if (res == 0) {
						var buffer = new Span<byte>(ogg_sync_buffer(&sync_in, new CLong(4096)), 4096);
						var numread = inputStream.Read(buffer);
						if (numread > 0) {
							ogg_sync_wrote(&sync_in, new CLong(numread));
							continue;
						}

						break;
					}

					if (ogg_page_eos(&page) == 1) {
						eos = true;
					}

					ogg_stream_pagein(&stream_in, &page);

					while (true) {
						res = ogg_stream_packetout(&stream_in, &packet);
						if (res == 0) {
							break;
						}

						if (res < 0) {
							continue;
						}

						var bs = vorbis_packet_blocksize(&vi, &packet);
						if (lastbs != 0) {
							granpos += (lastbs + bs) / 4;
						}

						lastbs = bs;

						packet.granulepos = granpos;
						packet.packetno = packetnum++;
						if (packet.e_o_s.Value == 0) {
							ogg_stream_packetin(&stream_out, &packet);
							opage = default;
							while (ogg_stream_pageout(&stream_out, &opage) > 0) {
								var header = new Span<byte>(opage.header, (int) opage.header_len.Value);
								var body = new Span<byte>(opage.body, (int) opage.body_len.Value);
								outputStream.Write(header);
								outputStream.Write(body);
							}
						}
					}
				}

				packet.e_o_s = new CLong(1);
				ogg_stream_packetin(&stream_out, &packet);
				opage = default;
				while (ogg_stream_flush(&stream_out, &opage) > 0) {
					var header = new Span<byte>(opage.header, (int) opage.header_len.Value);
					var body = new Span<byte>(opage.body, (int) opage.body_len.Value);
					outputStream.Write(header);
					outputStream.Write(body);
				}

				ogg_stream_clear(&stream_in);
				break;
			}
		} finally {
			ogg_stream_clear(&stream_out);
			vorbis_info_clear(&vi);
			ogg_sync_clear(&sync_in);
			ogg_sync_clear(&sync_out);
		}
	}

	[SuppressMessage("Performance", "CA1806:Do not ignore method results")]
	private static unsafe bool CopyHeaders(Stream fi, ogg_sync_state* si, ogg_stream_state* @is, Stream fo, ogg_sync_state* so, ogg_stream_state* os, vorbis_info* vi) {
		var buffer = new Span<byte>(ogg_sync_buffer(si, new CLong(4096)), 4096);
		var numread = fi.Read(buffer);
		ogg_sync_wrote(si, new CLong(numread));

		ogg_page page;
		if (ogg_sync_pageout(si, &page) != 1) {
			return false;
		}

		ogg_stream_init(@is, ogg_page_serialno(&page));
		ogg_stream_init(os, ogg_page_serialno(&page));

		if (ogg_stream_pagein(@is, &page) < 0) {
			ogg_stream_clear(@is);
			ogg_stream_clear(os);
			return false;
		}

		ogg_packet packet;
		if (ogg_stream_packetout(@is, &packet) != 1) {
			ogg_stream_clear(@is);
			ogg_stream_clear(os);
			return false;
		}

		vorbis_comment vc;
		vorbis_comment_init(&vc);
		if (vorbis_synthesis_headerin(vi, &vc, &packet) < 0) {
			vorbis_comment_clear(&vc);
			ogg_stream_clear(@is);
			ogg_stream_clear(os);
			return false;
		}

		ogg_stream_packetin(os, &packet);

		var i = 0;
		while (i < 2) {
			var res = ogg_sync_pageout(si, &page);

			switch (res) {
				case 0: {
					buffer = new Span<byte>(ogg_sync_buffer(si, new CLong(4096)), 4096);
					numread = fi.Read(buffer);
					if (numread == 0) {
						ogg_stream_clear(@is);
						ogg_stream_clear(os);
						return false;
					}

					ogg_sync_wrote(si, new CLong(4096));
					continue;
				}
				case 1: {
					ogg_stream_pagein(@is, &page);
					while (i < 2) {
						res = ogg_stream_packetout(@is, &packet);
						if (res == 0) {
							break;
						}

						if (res < 0) {
							vorbis_comment_clear(&vc);
							ogg_stream_clear(@is);
							ogg_stream_clear(os);
							return false;
						}

						vorbis_synthesis_headerin(vi, &vc, &packet);
						ogg_stream_packetin(os, &packet);
						i++;
					}

					break;
				}
			}
		}

		vorbis_comment_clear(&vc);

		while (ogg_stream_flush(os, &page) != 0) {
			var header = new Span<byte>(page.header, (int) page.header_len.Value);
			var body = new Span<byte>(page.body, (int) page.body_len.Value);
			fo.Write(header);
			fo.Write(body);
		}

		return true;
	}
}
