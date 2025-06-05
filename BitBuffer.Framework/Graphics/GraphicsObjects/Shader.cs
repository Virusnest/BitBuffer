using System;
using SDL3;

namespace BitBuffer.Framework.Graphics;

public class Shader : IGraphicsDisposable
{
  public readonly GraphicsState GraphicsState;
  public readonly GraphicsState.IGraphicsResource Resource;

  public Shader(GraphicsState graphicsState, ShaderInfo shaderInfo)
  {
    GraphicsState = graphicsState ?? throw new ArgumentNullException(nameof(graphicsState));
    Resource = graphicsState.CreateShader(shaderInfo);
  }


  public bool IsDisposed => Resource.Disposed;

  public void Dispose()
  {
    GraphicsState.DestroyObject(Resource);
    GC.SuppressFinalize(this);
  }

  public enum ShaderType
  {
    Vertex,
    Fragment,
    Compute,
  }

  public struct ShaderInfo
  {
    public string? IncludeDir;
    public string VertexSource;
    public string FragmentSource;

    public string VertexEntryPoint = "main";
    public string FragmentEntryPoint = "main";
    public ShaderInfo(string vertexSource, string fragmentSource, string? includeDir = null, string vertexEntryPoint = "main", string fragmentEntryPoint = "main")
    {
      IncludeDir = includeDir;
      VertexSource = vertexSource;
      FragmentSource = fragmentSource;
      VertexEntryPoint = vertexEntryPoint;
      FragmentEntryPoint = fragmentEntryPoint;
    }
    public ShaderInfo(string source, string? includeDir = null, string EntryPointPrefix = "main")
      : this(source, source, includeDir, $"{EntryPointPrefix}Vertex", $"{EntryPointPrefix}Fragment")
    {
    }

  }
}
