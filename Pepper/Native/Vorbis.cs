#pragma warning disable CA1815
#pragma warning disable CA1060
#pragma warning disable CA5393
#pragma warning disable CA5392

using System.Runtime.InteropServices;

// ReSharper disable InconsistentNaming

namespace Pepper.Native;

public unsafe struct vorbis_info {
	public int version;
	public int channels;
	public CLong rate;

	public CLong bitrate_upper;
	public CLong bitrate_nominal;
	public CLong bitrate_lower;
	public CLong bitrate_window;

	public void* codec_setup;
}

public unsafe struct vorbis_dsp_state {
	public int analysisp;
	public vorbis_info* vi;

	public float** pcm;
	public float** pcmret;
	public int pcm_storage;
	public int pcm_current;
	public int pcm_returned;

	public int preextrapolate;
	public int eofflag;

	public CLong lW;
	public CLong W;
	public CLong nW;
	public CLong centerW;

	public long granulepos;
	public long sequence;

	public long glue_bits;
	public long time_bits;
	public long floor_bits;
	public long res_bits;

	public void* backend_state;
}

public unsafe struct vorbis_block {
	public float** pcm;
	public oggpack_buffer obp;

	public CLong lW;
	public CLong W;
	public CLong nW;
	public int pcmend;
	public int mode;

	public int eofflag;
	public long granunlepos;
	public long sequence;
	public vorbis_dsp_state* vd;

	public void* localstore;
	public CLong localtop;
	public CLong localalloc;
	public CLong totaluse;
	public alloc_chain* reap;

	public CLong glue_bits;
	public CLong time_bits;
	public CLong floor_bits;
	public CLong res_bits;

	public void* @internal;
}

public unsafe struct alloc_chain {
	public void* ptr;
	public alloc_chain* next;
}

public unsafe struct vorbis_comment {
	public byte** user_comments;
	public int* comment_lengths;
	public int comments;
	public byte* vendor;
}

public static unsafe class Vorbis {
	private const string LibraryName = "libvorbis";

	public const int OV_FALSE = -1;
	public const int OV_EOF = -2;
	public const int OV_HOLE = -3;

	public const int OV_EREAD = -128;
	public const int OV_EFAULT = -129;
	public const int OV_EIMPL = -130;
	public const int OV_EINVAL = -131;
	public const int OV_ENOTVORBIS = -132;
	public const int OV_EBADHEADER = -133;
	public const int OV_EVERSION = -134;
	public const int OV_ENOTAUDIO = -135;
	public const int OV_EBADPACKET = -136;
	public const int OV_EBADLINK = -137;
	public const int OV_ENOSEEK = -138;


	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern void vorbis_info_init(vorbis_info* vi);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern void vorbis_info_clear(vorbis_info* vi);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern int vorbis_info_blocksize(vorbis_info* vi, int zo);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern void vorbis_comment_init(vorbis_comment* vc);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern void vorbis_comment_add(vorbis_comment* vc, byte* comment);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern void vorbis_comment_add_tag(vorbis_comment* vc, byte* tag, byte* contents);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern byte* vorbis_comment_query(vorbis_comment* vc, byte* tag, int count);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern int vorbis_comment_query_count(vorbis_comment* vc, byte* tag);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern void vorbis_comment_clear(vorbis_comment* vc);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern int vorbis_block_init(vorbis_dsp_state* v, vorbis_block* vb);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern int vorbis_block_clear(vorbis_block* vb);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern void vorbis_dsp_clear(vorbis_dsp_state* v);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern double vorbis_granule_time(vorbis_dsp_state* v, long granulepos);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern byte* vorbis_version_string();

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern int vorbis_analysis_init(vorbis_dsp_state* v, vorbis_info* vi);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern int vorbis_commentheader_out(vorbis_comment* vc, ogg_packet* op);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern int vorbis_analysis_headerout(vorbis_dsp_state* v, vorbis_comment* vc, ogg_packet* op, ogg_packet* op_comm, ogg_packet* op_code);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern float** vorbis_analysis_buffer(vorbis_dsp_state* v, int vals);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern int vorbis_analysis_wrote(vorbis_dsp_state* v, int vals);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern int vorbis_analysis_blockout(vorbis_dsp_state* v, vorbis_block* vb);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern int vorbis_analysis(vorbis_block* vb, ogg_packet* op);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern int vorbis_bitrate_addblock(vorbis_block* vb);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern int vorbis_bitrate_flushpacket(vorbis_dsp_state* vd, ogg_packet* op);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern int vorbis_synthesis_idheader(ogg_packet* op);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern int vorbis_synthesis_headerin(vorbis_info* vi, vorbis_comment* vc, ogg_packet* op);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern int vorbis_synthesis_init(vorbis_dsp_state* v, vorbis_info* vi);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern int vorbis_synthesis_restart(vorbis_dsp_state* v);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern int vorbis_synthesis(vorbis_block* vb, ogg_packet* op);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern int vorbis_synthesis_trackonly(vorbis_block* vb, ogg_packet* op);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern int vorbis_synthesis_blockin(vorbis_dsp_state* v, vorbis_block* vb);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern int vorbis_synthesis_pcmout(vorbis_dsp_state* v, float*** pcm);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern int vorbis_synthesis_lapout(vorbis_dsp_state* v, float*** pcm);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern int vorbis_synthesis_read(vorbis_dsp_state* v, int samples);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern long vorbis_packet_blocksize(vorbis_info* vi, ogg_packet* op);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern int vorbis_synthesis_halfrate(vorbis_info* v, int flag);

	[DllImport(LibraryName, ExactSpelling = true)]
	public static extern int vorbis_synthesis_halfrate_p(vorbis_info* v);
}
