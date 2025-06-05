using System.Runtime.CompilerServices;

namespace BitBuffer.Framework.Graphics;

public class GraphicsBuffer : IGraphicsDisposable
{
  public readonly GraphicsState.IGraphicsResource Resource;
  public readonly nint Size;

  public GraphicsState GraphicsState;

  public GraphicsBuffer(GraphicsState graphicsState)
  {
    GraphicsState = graphicsState;
    Resource = graphicsState.CreateBuffer();
  }

  public bool IsDisposed => Resource.Disposed;

  public void Dispose()
  {
    GraphicsState.DestroyObject(Resource);
    GC.SuppressFinalize(this);
  }

  public void Upload(nint data, nint size, nint offset = 0)
  {
    GraphicsState.UploadBufferData(Resource, data, size, offset);
  }
}

public class VertexBuffer : GraphicsBuffer
{
  public readonly VertexLayout Layout;

  public VertexBuffer(GraphicsState graphicsState, VertexLayout layout)
    : base(graphicsState)
  {
    Layout = layout;
  }
}
public class IndexBuffer : GraphicsBuffer
{
  public readonly IndexFormat Format;
  public IndexBuffer(GraphicsState graphicsState, IndexFormat format)
    : base(graphicsState)
  {
    Format = format;
  }
}
public class VertexBuffer<T> : VertexBuffer where T : unmanaged
{
  public VertexBuffer(GraphicsState graphicsState, VertexLayout layout)
    : base(graphicsState, layout)
  {
  }
  public unsafe void Upload(in ReadOnlySpan<T> data, nint offset = 0)
  {
    fixed (T* ptr = data)
    {
      Upload((nint)ptr, (Unsafe.SizeOf<T>() * data.Length), offset);
    }
  }
}