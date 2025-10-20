using System.Data;
using System.Runtime.CompilerServices;
using BitBuffer.Framework.Extensions;
using SDL3;

namespace BitBuffer.Framework.Graphics;

public class GraphicsStateSDLNew : GraphicsState{
  
  
  // SDL OBJECTS

  private nint _gpuDevice;
  private nint _renderCommandBuffer;
  private nint _uploadCommandBuffer;
  private nint renderPass = nint.Zero;
  private nint _context;
  private nint _window;

  #region GraphicsObjects
  private class GraphicsResource : IGraphicsResource
  {
    public bool Destroyed = false;
    public bool Disposed => Destroyed;
  }

  private class RenderTargetResource : GraphicsResource
  {
    public readonly List<TextureResource> Attachments = new();
  }

  private class TextureResource : GraphicsResource
  {
    public nint TextureHandle { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    
    public SDL.GPUTextureFormat Format { get; set; }

  }

  private class ShaderResource : GraphicsResource
  {
    public nint VertexHandle { get; set; }
    public nint FragmentHandle { get; set; }

  }
  private class BufferResource : GraphicsResource
  {
    public nint BufferHandle { get; set; }
    public SDL.GPUBufferUsageFlags Usage { get; set; }
    public uint Size { get; set; }
    public uint Capcaity { get; set; }
  }
  
  struct PipelineKey : IEquatable<PipelineKey>
  {
    public nint VertexShader;
    public nint FragmentShader;
    public SDL.GPUPrimitiveType PrimitiveType;
    public VertexLayout VertexLayout;
    public CullMode CullMode;
    public SDL.GPUFillMode FillMode;
    public bool EnableDepth;
    public SDL.GPUCompareOp DepthCompareOp;
    public bool EnableBlend;
    public BlendFactor SrcBlend, DstBlend;
    public uint RenderTargetFormat;  // or array of formats
    public uint DepthFormat;
    public uint SampleCount;

    public bool Equals(PipelineKey other) => 
      VertexShader == other.VertexShader &&
      FragmentShader == other.FragmentShader &&
      PrimitiveType == other.PrimitiveType &&
      VertexLayout == other.VertexLayout &&
      CullMode == other.CullMode &&
      FillMode == other.FillMode &&
      EnableDepth == other.EnableDepth &&
      DepthCompareOp == other.DepthCompareOp &&
      EnableBlend == other.EnableBlend &&
      SrcBlend == other.SrcBlend &&
      DstBlend == other.DstBlend &&
      RenderTargetFormat == other.RenderTargetFormat &&
      DepthFormat == other.DepthFormat &&
      SampleCount == other.SampleCount;

    public override int GetHashCode() {
      HashCode hash = new HashCode();
      hash.Add(VertexShader);
      hash.Add(FragmentShader);
      hash.Add(PrimitiveType);
      hash.Add(VertexLayout);
      hash.Add(CullMode);
      hash.Add(FillMode);
      hash.Add(EnableDepth);
      hash.Add(DepthCompareOp);
      hash.Add(EnableBlend);
      hash.Add(SrcBlend);
      hash.Add(DstBlend);
      hash.Add(RenderTargetFormat);
      hash.Add(DepthFormat);
      hash.Add(SampleCount);
      return hash.ToHashCode();
    }
  }
  #endregion
  
  Dictionary<PipelineKey, nint> pipelineCache;
  
  private void ResetCommandBuffer() {
    if (_renderCommandBuffer != nint.Zero|| _uploadCommandBuffer != nint.Zero) {
      throw new InvalidOperationException("Command buffers are already acquired.");
    }
    _renderCommandBuffer = SDL.AcquireGPUCommandBuffer(_gpuDevice);
    _uploadCommandBuffer = SDL.AcquireGPUCommandBuffer(_gpuDevice);
  }

  #region  LifeTime
  
  public override void Initialize(Window window) {
    ResetCommandBuffer();
  }

  public override void Shutdown() {
    throw new NotImplementedException();
  }

  public override void DestroyObject(IGraphicsResource resource) {
    throw new NotImplementedException();
  }
  #endregion

  public override void Clear(Colour colour) {
    throw new NotImplementedException();
  }

  public override void Present() {
    nint swapTex;
    uint w = 0, h = 0;
    SDL.WaitAndAcquireGPUSwapchainTexture(_renderCommandBuffer, _window, out swapTex, out w,  out h);
    throw new NotImplementedException();
  }


  const int MaxColorAttachments = 8;
  private unsafe nint CreatePipeline(DrawCommand command) {
    
    var colorBlendState = GetBlendState(command.BlendMode);
    var colorAttachments = stackalloc SDL.GPUColorTargetDescription[MaxColorAttachments];
    var colorAttachmentCount = 0;
    var vertexAttributeCount = 0;
    foreach (var vb in command.VertexBuffers)
      vertexAttributeCount += vb.Layout.Properties.Length;
    var depthStencilAttachment = SDL.GPUTextureFormat.Invalid;
    var vertexBindings = stackalloc SDL.GPUVertexBufferDescription[command.VertexBuffers.Count];
    var vertexAttributes = stackalloc SDL.GPUVertexAttribute[vertexAttributeCount];
    
    foreach (var format in GetDrawTargetFormats(currentRenderTarget))
    {
      if (IsDepthTextureFormat(format))
      {
        depthStencilAttachment = format;
      }
      else
      {
        colorAttachments[colorAttachmentCount] = new()
        {
          Format = format,
          BlendState = colorBlendState
        };
        colorAttachmentCount++;
      }
    }

    var attrbIndex = 0;
    for (int slot = 0; slot < command.VertexBuffers.Count; slot ++)
    {
      var it = command.VertexBuffers[slot];
      var instanceRate = command.InstanceInputRates[slot];
      var vertexOffset = 0;

      vertexBindings[slot] = new()
      {
        Slot = (uint)slot,
        Pitch = (uint)it.Layout.Stride,
        InputRate = instanceRate
          ? SDL.GPUVertexInputRate.Instance
          : SDL.GPUVertexInputRate.Vertex,
        InstanceStepRate = 0
      };

      foreach (var el in it.Layout.Properties)
      {
        vertexAttributes[attrbIndex] = new()
        {
          Location = (uint)el.Index,
          BufferSlot = (uint)slot,
          Format = GetVertexFormat(el.Type, el.Normalised),
          Offset = (uint)vertexOffset
        };
        vertexOffset += el.Type.SizeInBytes();
        attrbIndex++;
      }
    }

    var vertexInputState = new SDL.GPUVertexInputState() {
      NumVertexAttributes = (uint)vertexAttributeCount,
      NumVertexBuffers = (uint)command.VertexBuffers.Count,
      VertexAttributes = (IntPtr)vertexAttributes,
      VertexBufferDescriptions = (IntPtr)vertexBindings,
    };
    var rasterizerState = new SDL.GPURasterizerState
    {
      CullMode = command.CullMode switch
      {
        CullMode.Back => SDL.GPUCullMode.Back,
        CullMode.Front => SDL.GPUCullMode.Front,
        _ => SDL.GPUCullMode.None
      },
      FillMode = SDL.GPUFillMode.Fill,
      FrontFace = SDL.GPUFrontFace.CounterClockwise
    };

    // --- Depth/Stencil State ---
    var depthState = new SDL.GPUDepthStencilState
    {
      EnableDepthTest = (byte)(command.DepthTest?1:0),
      EnableDepthWrite = (byte)(command.DepthWrite?1:0),
      CompareOp = command.DepthCompare switch
      {
        DepthCompare.Less => SDL.GPUCompareOp.Less,
        DepthCompare.LessOrEqual => SDL.GPUCompareOp.LessOrEqual,
        DepthCompare.Greater => SDL.GPUCompareOp.Greater,
        DepthCompare.Always => SDL.GPUCompareOp.Always,
        DepthCompare.Equal => SDL.GPUCompareOp.Equal,
        _ => SDL.GPUCompareOp.Always
      }
    };
    SDL.GPUColorTargetDescription colorTargetDesc = new()
    {
      Format = GetTextureFormat(TextureFormat.R8G8B8A8),
      BlendState = GetBlendState(command.BlendMode)
    };

    SDL.GPUGraphicsPipelineTargetInfo targetInfo = new()
    {
      NumColorTargets = 1,
      ColorTargetDescriptions = (IntPtr)colorAttachments
    };

    // --- Create full pipeline info struct ---
    SDL.GPUGraphicsPipelineCreateInfo info = new()
    {
      TargetInfo = targetInfo,
      VertexInputState = vertexInputState,
      DepthStencilState = depthState,
      RasterizerState = rasterizerState,
      PrimitiveType = SDL.GPUPrimitiveType.TriangleList,
      VertexShader = nint.Zero,
      FragmentShader = nint.Zero
    };

    SDL.CreateGPUGraphicsPipeline(_gpuDevice, in info);
    return nint.Zero;
  }

  public override void PerformDraw(DrawCommand command) {
    
  }
  
  nint currentRenderPass = nint.Zero;
  RenderTarget? currentRenderTarget = null;
  
  public override bool BeginPass(RenderTarget? renderTarget, Colour? ClearColour, float? ClearDepth = 1.0f, uint? ClearStencil = 0) {
    if (currentRenderPass != nint.Zero)
      throw new InvalidOperationException("Render Pass already begun.");
    
    currentRenderTarget = renderTarget;
    var colorTargets = new List<nint>();
    var depthStencilTarget = nint.Zero;
    foreach (var it in renderTarget.Attachments)
    {
      var res = ((TextureResource)it.Resource).TextureHandle;

      // drawing to an invalid target
      if (it.IsDisposed || !it.IsTargetAttachment || res == nint.Zero)
        throw new Exception("Drawing to a Disposed or Invalid Texture");

      if (it.Format.IsDepthStencilFormat())
        depthStencilTarget = res;
      else
        colorTargets.Add(res);
    }

    SDL.GPUColorTargetInfo[] colorInfo = new SDL.GPUColorTargetInfo[colorTargets.Count];

    // get color infos
    for (int i = 0; i < colorTargets.Count; i++)
    {
      var col = ClearColour ?? Colour.Transparent;
      colorInfo[i] = new()
      {
        Texture = colorTargets[i],
        MipLevel = 0,
        LayerOrDepthPlane = 0,
        ClearColor = GetColor(col),
        LoadOp = ClearColour.HasValue ?
          SDL.GPULoadOp.Clear :
          SDL.GPULoadOp.Load,
        StoreOp = SDL.GPUStoreOp.Store,
        Cycle = (byte)(ClearColour.HasValue?1:0)
      };
    }
    
    var depthValue = new SDL.GPUDepthStencilTargetInfo();

    depthValue = new()
    {
      Texture = depthStencilTarget,
      ClearDepth = ClearDepth ?? 0,
      LoadOp = ClearDepth.HasValue ?
        SDL.GPULoadOp.Clear :
        SDL.GPULoadOp.Load,
      StoreOp = SDL.GPUStoreOp.Store,
      StencilLoadOp = ClearStencil.HasValue ?
        SDL.GPULoadOp.Clear :
        SDL.GPULoadOp.Load,
      StencilStoreOp = SDL.GPUStoreOp.Store,
      Cycle = (byte)(ClearDepth.HasValue && ClearStencil.HasValue?1:0),
      ClearStencil = (byte)(ClearStencil ?? 0),
    };

    currentRenderPass = SDL.BeginGPURenderPass(_renderCommandBuffer, in colorInfo, (uint)colorInfo.Length, in depthValue);
    return currentRenderPass!= nint.Zero;
  }

  public override void EndPass() {
    if (currentRenderPass == nint.Zero)
      throw new InvalidOperationException("No Render Pass begun.");
    SDL.EndGPURenderPass(currentRenderPass);
    currentRenderPass = nint.Zero;
  }

  #region CreateObjects
  
  public override void UploadBufferData(IGraphicsResource buffer, IntPtr data, IntPtr size, IntPtr offset) {
    throw new NotImplementedException();
  }

  public override IGraphicsResource CreateTexture(int width, int height, TextureFormat format, IGraphicsResource? RenderTarget = null) {
    throw new NotImplementedException();
  }

  public override IGraphicsResource CreateShader(Shader.ShaderInfo shaderInfo) {
    throw new NotImplementedException();
  }

  public override IGraphicsResource CreateBuffer(BufferType usage) {
    throw new NotImplementedException();
  }

  public override IGraphicsResource CreateVertexBuffer() {
    throw new NotImplementedException();
  }

  public override IGraphicsResource CreateIndexBuffer() {
    throw new NotImplementedException();
  }

  public override IGraphicsResource CreateRenderTarget() {
    throw new NotImplementedException();
  }

  #endregion
  
  #region Utils
  public override bool IsTextureFormatSupported(TextureFormat format) {
    throw new NotImplementedException();
  }
  
  private List<SDL.GPUTextureFormat> GetDrawTargetFormats(RenderTarget drawableTarget) {
    var target = drawableTarget;
    List<SDL.GPUTextureFormat> formats = new();
    foreach (var it in target.Attachments)
      formats.Add(GetTextureFormat(it.Format));
    return formats;
  }


  private static SDL.GPUVertexElementFormat GetVertexFormat(VertexType type, bool normalized)
  {
    return (type, normalized) switch
    {
      (VertexType.Float, _) => SDL.GPUVertexElementFormat.Float,
      (VertexType.Float2, _) => SDL.GPUVertexElementFormat.Float2,
      (VertexType.Float3, _) => SDL.GPUVertexElementFormat.Float3,
      (VertexType.Float4, _) => SDL.GPUVertexElementFormat.Float4,
      (VertexType.Byte4, false) => SDL.GPUVertexElementFormat.Byte4,
      (VertexType.Byte4, true) => SDL.GPUVertexElementFormat.Byte4Norm,
      (VertexType.UByte4, false) => SDL.GPUVertexElementFormat.Ubyte4,
      (VertexType.UByte4, true) => SDL.GPUVertexElementFormat.Ubyte4Norm,
      (VertexType.Short2, false) => SDL.GPUVertexElementFormat.Short2,
      (VertexType.Short2, true) => SDL.GPUVertexElementFormat.Short2Norm,
      (VertexType.UShort2, false) => SDL.GPUVertexElementFormat.Ushort2,
      (VertexType.UShort2, true) => SDL.GPUVertexElementFormat.Ushort2Norm,
      (VertexType.Short4, false) => SDL.GPUVertexElementFormat.Short4,
      (VertexType.Short4, true) => SDL.GPUVertexElementFormat.Short4Norm,
      (VertexType.UShort4, false) => SDL.GPUVertexElementFormat.Ushort4,
      (VertexType.UShort4, true) => SDL.GPUVertexElementFormat.Ushort4Norm,

      _ => throw new ArgumentException("Invalid Vertex Format", nameof(type)),
    };
  }


  private static SDL.GPUColorTargetBlendState GetBlendState(BlendMode blend)
  {
    SDL.GPUBlendFactor GetFactor(BlendFactor factor) => factor switch
    {
      BlendFactor.Zero => SDL.GPUBlendFactor.Zero,
      BlendFactor.One => SDL.GPUBlendFactor.One,
      BlendFactor.SrcColor => SDL.GPUBlendFactor.SrcColor,
      BlendFactor.OneMinusSrcColor => SDL.GPUBlendFactor.OneMinusSrcColor,
      BlendFactor.DstColor => SDL.GPUBlendFactor.DstColor,
      BlendFactor.OneMinusDstColor => SDL.GPUBlendFactor.OneMinusDstColor,
      BlendFactor.SrcAlpha => SDL.GPUBlendFactor.SrcAlpha,
      BlendFactor.OneMinusSrcAlpha => SDL.GPUBlendFactor.OneMinusSrcAlpha,
      BlendFactor.DstAlpha => SDL.GPUBlendFactor.DstAlpha,
      BlendFactor.OneMinusDstAlpha => SDL.GPUBlendFactor.OneMinusDstAlpha,
      BlendFactor.ConstantColor => SDL.GPUBlendFactor.ConstantColor,
      BlendFactor.OneMinusConstantColor => SDL.GPUBlendFactor.OneMinusConstantColor,
      BlendFactor.SrcAlphaSaturate => SDL.GPUBlendFactor.SrcAlphaSaturate,
      _ => throw new NotImplementedException()
    };

    SDL.GPUBlendOp GetOp(BlendOp op) => op switch
    {
      BlendOp.Add => SDL.GPUBlendOp.Add,
      BlendOp.Subtract => SDL.GPUBlendOp.Subtract,
      BlendOp.ReverseSubtract => SDL.GPUBlendOp.ReverseSubtract,
      BlendOp.Min => SDL.GPUBlendOp.Min,
      BlendOp.Max => SDL.GPUBlendOp.Max,
      _ => throw new NotImplementedException()
    };

    SDL.GPUColorComponentFlags GetFlags(BlendMask mask)
    {
      SDL.GPUColorComponentFlags flags = default;
      if (mask.Has(BlendMask.Red)) flags |= SDL.GPUColorComponentFlags.R;
      if (mask.Has(BlendMask.Green)) flags |= SDL.GPUColorComponentFlags.G;
      if (mask.Has(BlendMask.Blue)) flags |= SDL.GPUColorComponentFlags.B;
      if (mask.Has(BlendMask.Alpha)) flags |= SDL.GPUColorComponentFlags.A;
      return flags;
    }

    SDL.GPUColorTargetBlendState state = new()
    {
      EnableBlend = 1,
      SrcColorBlendfactor = GetFactor(blend.ColorSource),
      DstColorBlendfactor = GetFactor(blend.ColorDestination),
      ColorBlendOp = GetOp(blend.ColorOperation),
      SrcAlphaBlendfactor = GetFactor(blend.AlphaSource),
      DstAlphaBlendfactor = GetFactor(blend.AlphaDestination),
      AlphaBlendOp = GetOp(blend.AlphaOperation),
      ColorWriteMask = GetFlags(blend.Mask)
    };
    return state;
  }
  private bool isDepthFormat(TextureFormat format) =>
    format == TextureFormat.Depth16 ||
    format == TextureFormat.Depth24 ||
    format == TextureFormat.Depth32 ||
    format == TextureFormat.Depth24Stencil8 ||
    format == TextureFormat.Depth32Stencil8;

  private static bool IsDepthTextureFormat(SDL.GPUTextureFormat format) => format switch
  {
    SDL.GPUTextureFormat.D16Unorm => true,
    SDL.GPUTextureFormat.D24Unorm => true,
    SDL.GPUTextureFormat.D32Float => true,
    SDL.GPUTextureFormat.D24UnormS8Uint => true,
    SDL.GPUTextureFormat.D32FloatS8Uint => true,
    _ => false
  };
  private static SDL.GPUTextureFormat GetTextureFormat(TextureFormat format) => format switch
  {
    TextureFormat.R8G8B8A8 => SDL.GPUTextureFormat.R8G8B8A8Unorm,
    TextureFormat.R8 => SDL.GPUTextureFormat.R8Unorm,
    TextureFormat.R8G8 => SDL.GPUTextureFormat.R8G8Unorm,
    TextureFormat.Depth24Stencil8 => SDL.GPUTextureFormat.D24UnormS8Uint,
    TextureFormat.Depth32Stencil8 => SDL.GPUTextureFormat.D32FloatS8Uint,
    TextureFormat.Depth16 => SDL.GPUTextureFormat.D16Unorm,
    TextureFormat.Depth24 => SDL.GPUTextureFormat.D24Unorm,
    TextureFormat.Depth32 => SDL.GPUTextureFormat.D32Float,
    _ => throw new ArgumentException("Invalid Texture Format", nameof(format)),
  };
  private static SDL.FColor GetColor(Colour color)
  {
    var vec4 = color.ToVector4();
    return new() { R = vec4.X, G = vec4.Y, B = vec4.Z, A = vec4.W, };
  }
  #endregion
  
}