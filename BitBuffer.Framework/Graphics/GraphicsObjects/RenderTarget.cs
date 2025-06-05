using System;

namespace BitBuffer.Framework.Graphics;

public class RenderTarget : IGraphicsDisposable
{
  public int Width, Height;

  public GraphicsState.IGraphicsResource Resource;
  public readonly GraphicsState GraphicsState;

  public readonly Texture[] Attachments;

  public bool IsDisposed => Resource.Disposed;

  public RenderTarget(GraphicsState graphicsState, int width, int height, in Span<TextureFormat> colorAttachments)
  {
    Width = width;
    Height = height;
    GraphicsState = graphicsState;
    Attachments = new Texture[colorAttachments.Length];
    Resource = graphicsState.CreateRenderTarget();
    for (int i = 0; i < colorAttachments.Length; i++)
    {
      Attachments[i] = new Texture(graphicsState, width, height, colorAttachments[i], this);
    }
  }

  ~RenderTarget()
  {
    Dispose();
  }

  public void Dispose()
  {
    GC.SuppressFinalize(this);
    GraphicsState.DestroyObject(Resource);
  }
  public static implicit operator Texture(RenderTarget renderTarget)
  {
    if (renderTarget.Attachments.Length > 0)
      return renderTarget.Attachments[0];
    throw new InvalidOperationException("RenderTarget has no color attachments.");
  }
}
