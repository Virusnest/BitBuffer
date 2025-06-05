namespace BitBuffer.Framework.Graphics;

public enum IndexFormat
{

  Sixteen,

  ThirtyTwo
}
public static class IndexFormatExt
{
  public static int SizeInBytes(this IndexFormat format) => format switch
  {
    IndexFormat.Sixteen => 2,
    IndexFormat.ThirtyTwo => 4,
    _ => throw new NotImplementedException(),
  };

  public static IndexFormat GetFormatOf<T>() where T : unmanaged
  {
    if (typeof(T) == typeof(ushort)) return IndexFormat.Sixteen;
    if (typeof(T) == typeof(short)) return IndexFormat.Sixteen;
    if (typeof(T) == typeof(uint)) return IndexFormat.ThirtyTwo;
    if (typeof(T) == typeof(int)) return IndexFormat.ThirtyTwo;

    throw new Exception("Invalid Index Format Type");
  }
}