using BitBuffer.Framework.Graphics;

namespace BitBuffer.Framework.Graphics;

public class Texture : IGraphicsDisposable
{
  public readonly GraphicsState.IGraphicsResource Resource;

  public readonly GraphicsState GraphicsState;
  public readonly int Width;
  public readonly int Height;
  public TextureFormat Format;

  public Texture(GraphicsState graphicsState, int width, int height, TextureFormat format = TextureFormat.R8G8B8A8, RenderTarget? renderTarget = null)
  {
    GraphicsState = graphicsState;
    Width = width;
    Height = height;
    Format = format;
    Resource = graphicsState.CreateTexture(width, height, format, renderTarget?.Resource);
  }

  public bool IsDisposed => Resource.Disposed;

  public void Dispose()
  {
    GraphicsState.DestroyObject(Resource);
    GC.SuppressFinalize(this);
  }

  public struct TextureSampler
  {
    public Texture Texture;
    public TextureWrapping Wrapping;

    public TextureSampler(Texture texture, TextureWrapping wrapping = TextureWrapping.ClampToEdge)
    {
      Texture = texture;
      Wrapping = wrapping;
    }
  }
}