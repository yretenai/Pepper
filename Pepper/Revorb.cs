using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using OggVorbisSharp;
using static OggVorbisSharp.Ogg;
using static OggVorbisSharp.Vorbis;

namespace Pepper;

// ported from librevorb/revorb
// note: provide libogg.dll and libvorbis.dll on windows, rely on system libs for unix
[SuppressMessage("Performance", "CA1806:Do not ignore method results")]
public sealed record Revorb(WwiseRIFFVorbis Vorbis) : IDisposable {
	public void Dispose() {
		Vorbis.Dispose();
	}

	public void Decode(Stream outputStream) {
		using var memoryStream = new MemoryStream();
		Vorbis.Decode(memoryStream);
		memoryStream.Position = 0;
		Rebuild(memoryStream, outputStream);
	}

	public static unsafe void Rebuild(Stream inputStream, Stream outputStream) {
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
						} else {
							eos = true;
						}

						continue;
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
