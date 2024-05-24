using System.IO;
using Pepper.Structures;

namespace Pepper;

public record WwiseRIFFDummy : AbstractRIFFFile {
    public WwiseRIFFDummy(Stream stream, bool leaveOpen) : base(stream, leaveOpen) { }
    public override AudioFormat Format => AudioFormat.Wem;

    public override void Decode(Stream outputStream) {
        Stream.Position = 0;
        Stream.CopyTo(outputStream);
    }
}
