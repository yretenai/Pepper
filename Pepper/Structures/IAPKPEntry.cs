namespace Pepper.Structures;

public interface IAPKPEntry {
    public ulong Id { get; set; }
    public uint Alignment { get; set; }
    public int Size { get; set; }
    public int Offset { get; set; }
    public int Folder { get; set; }
}
