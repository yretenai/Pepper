using System.Collections.Generic;

namespace Pepper.Structures;

public interface IChunkedFile {
    public Dictionary<long, WAVEChunkFragment> Chunks { get; set; }
}
