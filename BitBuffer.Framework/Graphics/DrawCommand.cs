using System;
using BitBuffer.Framework.Util.MathUtils;

namespace BitBuffer.Framework.Graphics;

public struct DrawCommand
{
  public RenderTarget RenderTarget;
  public Material Material;
  public List<VertexBuffer> VertexBuffers;
  public List<bool> InstanceInputRates;
  public IndexBuffer? IndexBuffer;
  public int InstanceCount;
  public int IndexOffset;
  public int VertexOffset;
  public int VertexCount;
  public int IndexCount;
  public BlendMode BlendMode;
  public CullMode CullMode;
  public DepthCompare DepthCompare;

  public bool DepthTest;
  public bool DepthWrite;
  public bool ScissorTest;

  public RectI ScissorRect;
}
