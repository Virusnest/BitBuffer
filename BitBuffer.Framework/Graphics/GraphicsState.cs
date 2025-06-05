using TileGame.Graphics;

namespace BitBuffer.Framework.Graphics
{
  public abstract class GraphicsState
  {

    public Window? Window { get; protected set; }
    public interface IGraphicsResource
    {
      public bool Disposed { get; }
    }

    public abstract void Initialize(Window window);
    public abstract void Shutdown();
    public abstract void DestroyObject(IGraphicsResource resource);
    public abstract void Clear(Colour colour);
    public abstract void Present();
    public abstract void PerformDraw(DrawCommand command);
    public abstract void UploadBufferData(IGraphicsResource buffer, nint data, nint size, nint offset);
    public abstract IGraphicsResource CreateTexture(int width, int height, TextureFormat format, IGraphicsResource? RenderTarget = null);
    public abstract IGraphicsResource CreateShader(Shader.ShaderInfo shaderInfo);
    public abstract IGraphicsResource CreateBuffer();
    public abstract IGraphicsResource CreateVertexBuffer();
    public abstract IGraphicsResource CreateIndexBuffer();
    public abstract IGraphicsResource CreateRenderTarget();
    public abstract bool IsTextureFormatSupported(TextureFormat format);


  }
}