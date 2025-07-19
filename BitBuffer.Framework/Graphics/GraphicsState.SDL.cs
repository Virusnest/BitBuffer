using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BitBuffer.Framework.Extensions;
using BitBuffer.Framework.Util.MathUtils;
using SDL3;
using TileGame.Graphics;

namespace BitBuffer.Framework.Graphics;

public class GraphicsStateSDL : GraphicsState
{
  private nint _gpuDevice;

  private const int BufferTransferSize = 1024 * 1024; // 1MB buffer size for transfers

  private RenderTarget? BackBuffer;

  private Point2 backbufferSize;

  public const int MaxColorAttachments = 8;


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



  private readonly Dictionary<nint, GraphicsResource> _resources = new();


  public override void Clear(Colour colour)
  {
    throw new NotImplementedException();
  }

  public override IGraphicsResource CreateBuffer()
  {
    throw new NotImplementedException();
  }

  public override IGraphicsResource CreateRenderTarget()
  {
    return new RenderTargetResource();
  }

  public override IGraphicsResource CreateShader(Shader.ShaderInfo shaderInfo)
  {

    nint vertexShaderHandle = CompileShader(shaderInfo.VertexSource, ShaderCross.ShaderStage.Vertex, shaderInfo.IncludeDir, false, shaderInfo.VertexEntryPoint);
    nint fragmentShaderHandle = CompileShader(shaderInfo.FragmentSource, ShaderCross.ShaderStage.Fragment, shaderInfo.IncludeDir, false, shaderInfo.FragmentEntryPoint);


    return new ShaderResource
    {
      VertexHandle = vertexShaderHandle,
      FragmentHandle = fragmentShaderHandle,
    };
  }

  public override IGraphicsResource CreateTexture(int width, int height, TextureFormat format, IGraphicsResource? renderTarget = null)
  {
    if (width <= 0 || height <= 0)
    {
      throw new ArgumentException("Width and height must be greater than zero.");
    }
    SDL.GPUTextureFormat sdlFormat = GetTextureFormat(format);
    bool isDepthFormat = format == TextureFormat.Depth16 ||
                        format == TextureFormat.Depth24 ||
                        format == TextureFormat.Depth32 ||
                        format == TextureFormat.Depth24Stencil8 ||
                        format == TextureFormat.Depth32Stencil8;

    SDL.GPUTextureCreateInfo createInfo =
    new SDL.GPUTextureCreateInfo
    {
      Width = (uint)width,
      Height = (uint)height,
      Format = sdlFormat,
      Usage = SDL.GPUTextureUsageFlags.Sampler,
      NumLevels = 1,
      SampleCount = SDL.GPUSampleCount.SampleCount1,
    };
    if (renderTarget != null)
    {
      createInfo.Usage |= isDepthFormat
        ? SDL.GPUTextureUsageFlags.DepthStencilTarget
        : SDL.GPUTextureUsageFlags.ColorTarget;
    }

    nint textureHandle = SDL.CreateGPUTexture(_gpuDevice, createInfo);
    if (textureHandle == nint.Zero)
    {
      throw new InvalidOperationException($"Failed to create GPU texture. {SDL.GetError()}");
    }
    return new TextureResource
    {
      TextureHandle = textureHandle,
      Width = width,
      Height = height,
      Format = sdlFormat,
    };
  }

  public override IGraphicsResource CreateVertexBuffer()
  {
    return new BufferResource
    {
      Usage = SDL.GPUBufferUsageFlags.Vertex,
    };
  }

  nint _uploadBuffer;
  nint _downloadBuffer;
  nint _renderCommandBuffer;
  nint _uploadCommandBuffer;
  public override void Initialize(Window window)
  {
    if (window == null)
    {
      throw new ArgumentNullException(nameof(window), "Window cannot be null.");
    }
    Window = window;

    string GPU_API = "vulkan";
    SDL.GPUShaderFormat shaderFormat = SDL.GPUShaderFormat.SPIRV;
    if (OperatingSystem.IsMacOS())
    {
      GPU_API = "metal";
      shaderFormat = SDL.GPUShaderFormat.MSL;
    }
    else if (OperatingSystem.IsLinux())
    {
      GPU_API = "vulkan";
      shaderFormat = SDL.GPUShaderFormat.SPIRV;
    }
    else if (OperatingSystem.IsWindows())
    {
      GPU_API = "d3d11";
      shaderFormat = SDL.GPUShaderFormat.DXIL;
    }
    _gpuDevice = SDL.CreateGPUDevice(shaderFormat, false, GPU_API);
    if (_gpuDevice == nint.Zero)
    {
      throw new InvalidOperationException($"Failed to create GPU device. {SDL.GetError()}");
    }
    if (!SDL.ClaimWindowForGPUDevice(_gpuDevice, window.Handle))
    {
      throw new InvalidOperationException($"Failed to claim window for GPU device. {SDL.GetError()}");
    }
    _uploadBuffer = SDL.CreateGPUTransferBuffer(_gpuDevice, new SDL.GPUTransferBufferCreateInfo
    {
      Size = BufferTransferSize,
      Usage = SDL.GPUTransferBufferUsage.Upload
    });
    if (_uploadBuffer == nint.Zero)
    {
      throw new InvalidOperationException($"Failed to create transfer buffer. {SDL.GetError()}");
    }
    _downloadBuffer = SDL.CreateGPUTransferBuffer(_gpuDevice, new SDL.GPUTransferBufferCreateInfo
    {
      Size = BufferTransferSize,
      Usage = SDL.GPUTransferBufferUsage.Download
    });
    if (_downloadBuffer == nint.Zero)
    {
      throw new InvalidOperationException($"Failed to create download buffer. {SDL.GetError()}");
    }

  }
  public override void Present()
  {
    if (SDL.WaitAndAcquireGPUSwapchainTexture(_gpuDevice, Window?.Handle ?? nint.Zero, out nint swapchainTexture, out uint width, out uint height))
    {
      if (swapchainTexture != nint.Zero && width > 0 && height > 0 && BackBuffer != null)
      {
        SDL.GPUBlitInfo blitInfo = new SDL.GPUBlitInfo
        {
          Source = new()
          {
            Texture = ((TextureResource)BackBuffer.Attachments[0].Resource).TextureHandle,
            MipLevel = 0,
            LayerOrDepthPlane = 0,
            X = 0,
            Y = 0,
            W = Math.Min(width, (uint)BackBuffer.Width),
            H = Math.Min(height, (uint)BackBuffer.Height),
          },
          Destination = new()
          {
            Texture = swapchainTexture,
            MipLevel = 0,
            LayerOrDepthPlane = 0,
            X = 0,
            Y = 0,
            W = Math.Min(width, (uint)BackBuffer.Width),
            H = Math.Min(height, (uint)BackBuffer.Height),
          },
          LoadOp = SDL.GPULoadOp.Clear,
          FlipMode = SDL.FlipMode.None,
          Filter = SDL.GPUFilter.Nearest,
          Cycle = 0,
        };
        SDL.BlitGPUTexture(_renderCommandBuffer, blitInfo);


      }
      backbufferSize = new Point2((int)width, (int)height);
      if (BackBuffer == null || BackBuffer.Width < backbufferSize.X || BackBuffer.Height < backbufferSize.Y)
      {
        BackBuffer?.Dispose();
        BackBuffer = new(this, backbufferSize.X + 64, backbufferSize.Y + 64, [TextureFormat.Color]);
      }
      // resize buffer if it's too large
      else if (BackBuffer.Width > backbufferSize.X + 128 || BackBuffer.Height > backbufferSize.Y + 128)
      {
        BackBuffer?.Dispose();
        BackBuffer = new(this, backbufferSize.X, backbufferSize.Y, [TextureFormat.Color]);
      }
    }
    else
    {
      throw new InvalidOperationException($"Failed to wait for GPU device. {SDL.GetError()}");
    }
  }

  public override void Shutdown()
  {
    throw new NotImplementedException();
  }

  public override IGraphicsResource CreateIndexBuffer()
  {
    return new BufferResource
    {
      Usage = SDL.GPUBufferUsageFlags.Index,
    };
  }

  public override bool IsTextureFormatSupported(TextureFormat format)
  {
    throw new NotImplementedException();
  }

  public override void DestroyObject(IGraphicsResource resource)
  {
    switch (resource)
    {
      case TextureResource texture:
        DestroyTexture(texture);
        break;
      case ShaderResource shader:
        DestroyShader(shader);
        break;
      case BufferResource buffer:
        DestroyBuffer(buffer);
        break;
      case RenderTargetResource framebuffer:
        DestroyFramebuffer(framebuffer);
        break;
      default:
        throw new NotSupportedException($"Unsupported resource type: {resource.GetType()}");
    }
  }
  private void DestroyTexture(TextureResource texture)
  {
    if (texture.Disposed)
    {
      return;
    }
    SDL.ReleaseGPUTexture(_gpuDevice, texture.TextureHandle);
    texture.Destroyed = true;
  }
  private void DestroyShader(ShaderResource shader)
  {
    if (shader.Disposed)
    {
      return;
    }
    SDL.ReleaseGPUShader(_gpuDevice, shader.VertexHandle);
    SDL.ReleaseGPUShader(_gpuDevice, shader.FragmentHandle);
    shader.Destroyed = true;
  }
  private void DestroyBuffer(BufferResource buffer)
  {
    if (buffer.Disposed)
    {
      return;
    }
    SDL.ReleaseGPUBuffer(_gpuDevice, buffer.BufferHandle);
    buffer.Destroyed = true;
  }
  private void DestroyFramebuffer(RenderTargetResource framebuffer)
  {
    if (framebuffer.Disposed)
    {
      return;
    }
    foreach (var attachment in framebuffer.Attachments)
    {
      DestroyTexture(attachment);
    }
    framebuffer.Destroyed = true;
  }

  public override void UploadBufferData(IGraphicsResource buffer, nint data, nint size, nint offset)
  {
    var bufferResource = buffer as BufferResource;
    if (bufferResource == null || bufferResource.Disposed)
    {
      throw new InvalidOperationException("Buffer resource is not valid or has been disposed.");
    }
    bool needsResizing = bufferResource.Size < (size + offset);
    if (bufferResource.BufferHandle == nint.Zero | needsResizing)
    {
      SDL.GPUBufferCreateInfo createInfo = new SDL.GPUBufferCreateInfo
      {
        Usage = bufferResource.Usage,
        Size = (uint)(size + offset),
      };
      bufferResource.BufferHandle = SDL.CreateGPUBuffer(_gpuDevice, createInfo);
      if (bufferResource.BufferHandle == nint.Zero)
      {
        throw new InvalidOperationException($"Failed to create GPU buffer. {SDL.GetError()}");
      }
      bufferResource.Size = (uint)(size + offset);
    }
    nint uploadBuffer = _uploadBuffer;
    bool shouldCycle = true;
    if (size + offset >= BufferTransferSize)
    {
      // If the size exceeds the transfer buffer size, we need to create a new transfer buffer
      uploadBuffer = SDL.CreateGPUTransferBuffer(_gpuDevice, new SDL.GPUTransferBufferCreateInfo
      {
        Size = (uint)(size + offset),
        Usage = SDL.GPUTransferBufferUsage.Upload
      });
      if (uploadBuffer == nint.Zero)
      {
        throw new InvalidOperationException($"Failed to create transfer buffer. {SDL.GetError()}");
      }
      shouldCycle = false; // We won't cycle this buffer since it's a new one
    }
    unsafe
    {
      nint ptr = SDL.MapGPUTransferBuffer(_gpuDevice, uploadBuffer, shouldCycle);
      Buffer.MemoryCopy(data.ToPointer(), ptr.ToPointer(), size, size);
      SDL.UnmapGPUTransferBuffer(_gpuDevice, uploadBuffer);
    }
    nint copyPass = SDL.BeginGPUCopyPass(_uploadCommandBuffer);
    if (copyPass == nint.Zero)
    {
      throw new InvalidOperationException($"Failed to begin GPU copy pass. {SDL.GetError()}");
    }
    SDL.UploadToGPUBuffer(copyPass, new SDL.GPUTransferBufferLocation
    {
      TransferBuffer = uploadBuffer,
      Offset = 0,
    }, new SDL.GPUBufferRegion
    {
      Buffer = bufferResource.BufferHandle,
      Offset = (uint)offset,
      Size = (uint)size,
    }, shouldCycle);
    SDL.EndGPUCopyPass(copyPass);
    if (!shouldCycle)
    {
      SDL.ReleaseGPUTransferBuffer(_gpuDevice, uploadBuffer);
    }
  }
  private nint CompileShader(string source, ShaderCross.ShaderStage shaderStage, string? includeDir = null, bool enableDebug = false, string Entrypoint = "main")
  {
    var hlslinfo = new ShaderCross.HLSLInfo
    {
      Entrypoint = Entrypoint,
      ShaderStage = ShaderCross.ShaderStage.Vertex,
      IncludeDir = "",
      Props = 0,
      Name = "Pain.hlsl",
      Defines = nint.Zero,
      Source = source,
      EnableDebug = false
    };
    var shaderHandle = ShaderCross.CompileGraphicsShaderFromHLSL(_gpuDevice, in hlslinfo, out var metadata);
    if (shaderHandle == nint.Zero)
    {
      throw new InvalidOperationException($"Failed to compile shader. {SDL.GetError()}");
    }

    return shaderHandle;


  }
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

  private void EndRenderPass()
  {
    SDL.EndGPURenderPass(_renderCommandBuffer);
  }

  private void FlushCommands()
  {
    nint[] fences = new nint[2];
    fences[0] = SDL.SubmitGPUCommandBufferAndAcquireFence(_uploadCommandBuffer);
    fences[1] = SDL.SubmitGPUCommandBufferAndAcquireFence(_renderCommandBuffer);

    SDL.WaitForGPUFences(_gpuDevice, true, fences, (uint)fences.Length);
    SDL.ReleaseGPUFence(_gpuDevice, fences[0]);
    SDL.ReleaseGPUFence(_gpuDevice, fences[1]);
  }

  private nint renderPass;
  private unsafe bool BeginRenderPass(RenderTarget renderTarget, Colour? ClearColour, float? ClearDepth = 1.0f, uint? ClearStencil = 0)
  {
    List<nint> ColourTargets = new();
    Span<SDL.GPUColorTargetInfo> colorInfo = stackalloc SDL.GPUColorTargetInfo[ColourTargets.Count];
    var depthStencilTarget = nint.Zero;

    foreach (var it in renderTarget.Attachments)
    {
      var res = ((TextureResource)it.Resource).TextureHandle;

      // drawing to an invalid target
      if (it.IsDisposed || res == nint.Zero)
        throw new Exception("Drawing to a Disposed or Invalid Texture");

      if (it.Format.IsDepthStencilFormat())
        depthStencilTarget = res;
      else
        ColourTargets.Add(res);


    }
    // get color infos
    for (int i = 0; i < ColourTargets.Count; i++)
    {
      var col = ClearColour;
      colorInfo[i] = new()
      {
        Texture = ColourTargets[i],
        MipLevel = 0,
        LayerOrDepthPlane = 0,
        ClearColor = GetClearColor(col),
        LoadOp = ClearColour.HasValue ?
          SDL.GPULoadOp.Clear :
          SDL.GPULoadOp.Load,
        StoreOp = SDL.GPUStoreOp.Store,
        Cycle = ClearColour.HasValue ? (byte)1 : (byte)0
      };
    }
    var depthValue = new SDL.GPUDepthStencilTargetInfo();
    scoped ref var depthTarget = ref Unsafe.NullRef<SDL.GPUDepthStencilTargetInfo>();
    if (depthStencilTarget != nint.Zero)
    {
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
        Cycle = (ClearDepth.HasValue && ClearStencil.HasValue) ? (byte)1 : (byte)0,
        ClearStencil = (byte)(ClearStencil ?? 0),
      };
      depthTarget = ref depthValue;
    }

    nint colourinfoPtr = nint.Zero;
    if (ColourTargets.Count > 0)
    {
      colourinfoPtr = (nint)Unsafe.AsPointer(ref colorInfo[0]);
    }
    else
    {
      colourinfoPtr = nint.Zero;
    }
    renderPass = SDL.BeginGPURenderPass(_renderCommandBuffer, colourinfoPtr, (uint)ColourTargets.Count, depthTarget);
    return renderPass != nint.Zero;
  }
  private SDL.FColor GetClearColor(Colour? clearColour)
  {
    if (clearColour.HasValue)
    {
      return new SDL.FColor(clearColour.Value.R, clearColour.Value.G, clearColour.Value.B, clearColour.Value.A);
    }
    return new SDL.FColor(0, 0, 0, 1); // Default to black
  }

  private Dictionary<int, nint> pipeline_cache = new();

  private nint CreateOrGetPipeline(DrawCommand command)
  {

    //hash the drawcommand to use as a key
    var hash = HashCode.Combine(
      command.Material.Shader?.Resource,
      command.CullMode,
      command.DepthCompare,
      command.DepthTest,
      command.DepthWrite,
      command.BlendMode
    );
    if (command.IndexBuffer != null)
      hash = HashCode.Combine(hash, command.IndexBuffer.Format);

    foreach (var vb in command.VertexBuffers)
      hash = HashCode.Combine(hash, vb.Layout);
    foreach (var ir in command.InstanceInputRates)
      hash = HashCode.Combine(hash, ir);
    // combine with target attachment formats
    foreach (var format in GetDrawTargetFormats(command.RenderTarget))
      hash = HashCode.Combine(hash, format);
    if (pipeline_cache.TryGetValue(hash, out var pipeline))
    {
      return pipeline;
    }

    // Create a new pipeline if it doesn't exist
    pipeline = CreatePipeline(command);
    pipeline_cache[hash] = pipeline;
    return pipeline;
  }

  private unsafe nint CreatePipeline(DrawCommand command)
  {
    var shaderRes = (ShaderResource)command.Material.Shader!.Resource;
    var vertexAttributeCount = 0;
    foreach (var vb in command.VertexBuffers)
      vertexAttributeCount += vb.Layout.Properties.Length;

    var colorBlendState = GetBlendState(command.BlendMode);
    var colorAttachments = stackalloc SDL.GPUColorTargetDescription[MaxColorAttachments];
    var colorAttachmentCount = 0;
    var depthStencilAttachment = SDL.GPUTextureFormat.Invalid;
    var vertexBindings = stackalloc SDL.GPUVertexBufferDescription[command.VertexBuffers.Count];
    var vertexAttributes = stackalloc SDL.GPUVertexAttribute[vertexAttributeCount];

    foreach (var format in GetDrawTargetFormats(command.RenderTarget))
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
    for (int slot = 0; slot < command.VertexBuffers.Count; slot++)
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

    SDL.GPUGraphicsPipelineCreateInfo info = new()
    {
      VertexShader = shaderRes.VertexHandle,
      FragmentShader = shaderRes.FragmentHandle,
      VertexInputState = new()
      {
        VertexBufferDescriptions = (nint)vertexBindings,
        NumVertexBuffers = (uint)command.VertexBuffers.Count,
        VertexAttributes = (nint)vertexAttributes,
        NumVertexAttributes = (uint)vertexAttributeCount
      },
      PrimitiveType = SDL.GPUPrimitiveType.TriangleList,
      RasterizerState = new()
      {
        FillMode = SDL.GPUFillMode.Fill,
        CullMode = command.CullMode switch
        {
          CullMode.None => SDL.GPUCullMode.None,
          CullMode.Front => SDL.GPUCullMode.Front,
          CullMode.Back => SDL.GPUCullMode.Back,
          _ => throw new NotImplementedException()
        },
        FrontFace = SDL.GPUFrontFace.Clockwise,
        EnableDepthBias = 0
      },
      MultisampleState = new()
      {
        SampleCount = SDL.GPUSampleCount.SampleCount1,
        SampleMask = 0
      },
      DepthStencilState = new()
      {
        CompareOp = command.DepthCompare switch
        {
          DepthCompare.Always => SDL.GPUCompareOp.Always,
          DepthCompare.Never => SDL.GPUCompareOp.Never,
          DepthCompare.Less => SDL.GPUCompareOp.Less,
          DepthCompare.Equal => SDL.GPUCompareOp.Equal,
          DepthCompare.LessOrEqual => SDL.GPUCompareOp.LessOrEqual,
          DepthCompare.Greater => SDL.GPUCompareOp.Greater,
          DepthCompare.NotEqual => SDL.GPUCompareOp.NotEqual,
          DepthCompare.GreaterOrEqual => SDL.GPUCompareOp.GreaterOrEqual,
          _ => SDL.GPUCompareOp.Never
        },
        BackStencilState = default,
        FrontStencilState = default,
        CompareMask = 0xFF,
        WriteMask = 0xFF,
        EnableDepthTest = command.DepthTest ? (byte)1 : (byte)0,
        EnableDepthWrite = command.DepthWrite ? (byte)1 : (byte)0,
        EnableStencilTest = 0, // TODO: allow this
      },
      TargetInfo = new()
      {
        ColorTargetDescriptions = (nint)colorAttachments,
        NumColorTargets = (uint)colorAttachmentCount,
        HasDepthStencilTarget = depthStencilAttachment != SDL.GPUTextureFormat.Invalid ?
          (byte)1 : (byte)0,
        DepthStencilFormat = depthStencilAttachment
      }
    };

    var pipeline = SDL.CreateGPUGraphicsPipeline(_gpuDevice, info);
    if (pipeline == nint.Zero)
      throw new InvalidOperationException($"Failed to create GPU graphics pipeline. {SDL.GetError()}");
    return pipeline;

  }

  public override void PerformDraw(DrawCommand command)
  {
    if (command.RenderTarget == null)
    {
      throw new ArgumentNullException(nameof(command.RenderTarget), "Render target cannot be null.");
    }
    if (command.Material.Shader == null || command.Material.Shader.Resource == null)
    {
      throw new ArgumentNullException(nameof(command.Material.Shader), "Shader cannot be null.");
    }

    if (!BeginRenderPass(command.RenderTarget, default))
      return;

    nint pipeline = CreateOrGetPipeline(command);
    SDL.BindGPUGraphicsPipeline(renderPass, pipeline);

    // Bind vertex buffers

    SDL.GPUBufferBinding[] vertexBindings = new SDL.GPUBufferBinding[command.VertexBuffers.Count];
    for (int i = 0; i < command.VertexBuffers.Count; i++)
    {
      vertexBindings[i] = new()
      {
        Buffer = ((BufferResource)command.VertexBuffers[i].Resource).BufferHandle,
        Offset = 0,
      };
    }
    SDL.BindGPUVertexBuffers(renderPass, 0, vertexBindings, (uint)command.VertexBuffers.Count);


    // Bind index buffer if present
    if (command.IndexBuffer != null)
    {
      var ib = (BufferResource)command.IndexBuffer.Resource;
      SDL.BindGPUIndexBuffer(_renderCommandBuffer, new SDL.GPUBufferBinding
      {
        Buffer = ib.BufferHandle,
        Offset = (uint)command.IndexOffset,
      }, command.IndexBuffer.Format switch
      {
        IndexFormat.Sixteen => SDL.GPUIndexElementSize.IndexElementSize16Bit,
        IndexFormat.ThirtyTwo => SDL.GPUIndexElementSize.IndexElementSize32Bit,
        _ => throw new NotImplementedException()
      });
    }

    EndRenderPass();
    FlushCommands();
    if (command.IndexBuffer == null)
    {
      SDL.DrawGPUPrimitives(renderPass, (uint)command.VertexCount, (uint)Math.Max(command.InstanceCount, 1), (uint)command.VertexOffset, 0);
    }
    else
    {
      SDL.DrawGPUIndexedPrimitives(renderPass, (uint)command.IndexCount, (uint)Math.Max(command.InstanceCount, 1), (uint)command.IndexOffset, (short)command.VertexOffset, 0);
    }
  }
  private List<SDL.GPUTextureFormat> GetDrawTargetFormats(RenderTarget drawableTarget)
  {
    List<SDL.GPUTextureFormat> formats = new();
    foreach (var it in drawableTarget.Attachments)
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

}
