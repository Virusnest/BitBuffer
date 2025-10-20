using System;
using BitBuffer.Framework.Util.MathUtils;

namespace BitBuffer.Framework.Graphics;

public struct DrawCommand
{
  public Material? Material;
  public List<VertexBuffer> VertexBuffers = new List<VertexBuffer>();
  public List<bool> InstanceInputRates = new List<bool>();
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
  public RectI? Viewport;

  public DrawCommand() {
    Material = null;
    IndexBuffer = null;
    InstanceCount = 0;
    IndexOffset = 0;
    VertexOffset = 0;
    VertexCount = 0;
    IndexCount = 0;
    BlendMode = BlendMode.NonPremultiplied;
    CullMode = CullMode.None;
    DepthCompare = DepthCompare.Always;
    DepthTest = false;
    DepthWrite = false;
    ScissorTest = false;
    ScissorRect = default;
  }
}
