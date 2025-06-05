using System.Runtime.CompilerServices;

namespace BitBuffer.Framework.Graphics;

public class VertexLayout(uint size, VertexLayout.VertexAttribute[] properties)
{
  public readonly VertexAttribute[] Properties = properties;
  public readonly uint Stride = size;

  public static VertexLayout CreateLayout<T>(params VertexAttribute[] properties) where T : struct
  {
    return new VertexLayout((uint)Unsafe.SizeOf<T>(), properties);
  }


  public struct VertexAttribute(uint index, int size, VertexType type, bool normalised)
  {
    public VertexType Type = type;
    public int Size = size;
    public uint Index = index;
    public bool Normalised = normalised;
  }
}