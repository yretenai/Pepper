#pragma warning disable CA1815
#pragma warning disable CA1060
#pragma warning disable CA5393
#pragma warning disable CA5392

using System.Runtime.InteropServices;

// ReSharper disable InconsistentNaming

namespace Pepper.Native;

public unsafe struct ogg_iovec_t {
	public void* iov_base;
	public nuint iov_len;
}

public unsafe struct oggpack_buffer {
	public CLong endbyte;
	public int endbit;
	public byte* buffer;
	public byte* ptr;
	public CLong storage;
}

public unsafe struct ogg_page {
	public byte* header;
	public CLong header_len;
	public byte* body;
	public CLong body_len;
}

public unsafe struct ogg_stream_state {
	public byte* body_data;
	public CLong body_storage;
	public CLong body_fill;
	public CLong body_returned;
	public int* lacing_vals;
	public long* granule_vals;
	public CLong lacing_storage;
	public CLong lacing_fill;
	public CLong lacing_packet;
	public CLong lacing_returned;
	public fixed byte header[282];
	public int header_fill;
	public int e_o_s;
	public int b_o_s;
	public CLong serialno;
	public CLong pageno;
	public long packetno;
	public long granulepos;
}

public unsafe struct ogg_packet {
	public byte* packet;
	public CLong bytes;
	public CLong b_o_s;
	public CLong e_o_s;
	public long granulepos;
	public long packetno;
}

public unsafe struct ogg_sync_state {
	public byte* data;
	public int storage;
	public int fill;
	public int returned;
	public int unsynced;
	public int headerbytes;
	public int bodybytes;
}

public static unsafe class Ogg {
	private const string LibraryName = "libogg";

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern void oggpack_writeinit(oggpack_buffer* b);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern int oggpack_writecheck(oggpack_buffer* b);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern void oggpack_writetrunc(oggpack_buffer* b, CLong bits);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern void oggpack_writealign(oggpack_buffer* b);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern void oggpack_writecopy(oggpack_buffer* b, void* source, CLong bits);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern void oggpack_reset(oggpack_buffer* b);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern void oggpack_writeclear(oggpack_buffer* b);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern void oggpack_readinit(oggpack_buffer* b, byte* buf, int bytes);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern void oggpack_write(oggpack_buffer* b, CULong value, int bits);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern CLong oggpack_look(oggpack_buffer* b, int bits);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern CLong oggpack_look1(oggpack_buffer* b);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern void oggpack_adv(oggpack_buffer* b, int bits);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern void oggpack_adv1(oggpack_buffer* b);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern CLong oggpack_read(oggpack_buffer* b, int bits);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern CLong oggpack_read1(oggpack_buffer* b);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern CLong oggpack_bytes(oggpack_buffer* b);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern CLong oggpack_bits(oggpack_buffer* b);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern byte* oggpack_get_buffer(oggpack_buffer* b);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern void oggpackB_writeinit(oggpack_buffer* b);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern int oggpackB_writecheck(oggpack_buffer* b);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern void oggpackB_writetrunc(oggpack_buffer* b, CLong bits);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern void oggpackB_writealign(oggpack_buffer* b);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern void oggpackB_writecopy(oggpack_buffer* b, void* source, CLong bits);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern void oggpackB_reset(oggpack_buffer* b);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern void oggpackB_writeclear(oggpack_buffer* b);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern void oggpackB_readinit(oggpack_buffer* b, byte* buf, int bytes);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern void oggpackB_write(oggpack_buffer* b, CULong value, int bits);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern CLong oggpackB_look(oggpack_buffer* b, int bits);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern CLong oggpackB_look1(oggpack_buffer* b);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern void oggpackB_adv(oggpack_buffer* b, int bits);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern void oggpackB_adv1(oggpack_buffer* b);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern CLong oggpackB_read(oggpack_buffer* b, int bits);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern CLong oggpackB_read1(oggpack_buffer* b);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern CLong oggpackB_bytes(oggpack_buffer* b);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern CLong oggpackB_bits(oggpack_buffer* b);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern byte* oggpackB_get_buffer(oggpack_buffer* b);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern int ogg_stream_packetin(ogg_stream_state* os, ogg_packet* op);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern int ogg_stream_iovecin(ogg_stream_state* os, ogg_iovec_t* iov, int count, CLong e_o_s, long granulepos);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern int ogg_stream_pageout(ogg_stream_state* os, ogg_page* og);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern int ogg_stream_pageout_fill(ogg_stream_state* os, ogg_page* og, int nfill);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern int ogg_stream_flush(ogg_stream_state* os, ogg_page* og);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern int ogg_stream_flush_fill(ogg_stream_state* os, ogg_page* og, int nfill);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern int ogg_sync_init(ogg_sync_state* oy);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern int ogg_sync_clear(ogg_sync_state* oy);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern int ogg_sync_reset(ogg_sync_state* oy);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern int ogg_sync_destroy(ogg_sync_state* oy);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern int ogg_sync_check(ogg_sync_state* oy);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern byte* ogg_sync_buffer(ogg_sync_state* oy, CLong size);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern int ogg_sync_wrote(ogg_sync_state* oy, CLong bytes);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern CLong ogg_sync_pageseek(ogg_sync_state* oy, ogg_page* og);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern int ogg_sync_pageout(ogg_sync_state* oy, ogg_page* og);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern int ogg_stream_pagein(ogg_stream_state* os, ogg_page* og);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern int ogg_stream_packetout(ogg_stream_state* os, ogg_packet* op);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern int ogg_stream_packetpeek(ogg_stream_state* os, ogg_packet* op);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern int ogg_stream_init(ogg_stream_state* os, int serialno);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern int ogg_stream_clear(ogg_stream_state* os);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern int ogg_stream_reset(ogg_stream_state* os);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern int ogg_stream_reset_serialno(ogg_stream_state* os, int serialno);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern int ogg_stream_destroy(ogg_stream_state* os);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern int ogg_stream_check(ogg_stream_state* os);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern int ogg_stream_eos(ogg_stream_state* os);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern void ogg_page_checksum_set(ogg_page* og);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern int ogg_page_version( /*const*/ ogg_page* og);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern int ogg_page_continued( /*const*/ ogg_page* og);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern int ogg_page_bos( /*const*/ ogg_page* og);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern int ogg_page_eos( /*const*/ ogg_page* og);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern long ogg_page_granulepos( /*const*/ ogg_page* og);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern int ogg_page_serialno( /*const*/ ogg_page* og);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern CLong ogg_page_pageno( /*const*/ ogg_page* og);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern int ogg_page_packets( /*const*/ ogg_page* og);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern void ogg_packet_clear(ogg_packet* op);
}
