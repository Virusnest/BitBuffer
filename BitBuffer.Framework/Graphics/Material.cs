using System;
using System.Runtime.CompilerServices;
using static BitBuffer.Framework.Graphics.Texture;

namespace BitBuffer.Framework.Graphics;

public class Material
{
  public Shader? Shader;

  public MaterialProperties VertexProperties = new();
  public MaterialProperties FragmentProperties = new();


  public class MaterialProperties
  {
    public const int MaxUniformBuffers = 8;
    public readonly Sampler[] Samplers = new Sampler[16];
    public readonly byte[][] UniformData = [.. Enumerable.Range(0, MaxUniformBuffers).Select(it => Array.Empty<byte>())];
    public unsafe void SetUniformData<T>(in T data, int slot) where T : unmanaged
    {
      fixed (T* ptr = &data)
      {
        SetUniformData(new ReadOnlySpan<byte>((byte*)ptr, Unsafe.SizeOf<T>()), slot);
      }
    }

    public unsafe void SetUniformData(ReadOnlySpan<byte> data, int slot)
    {
      if (data.Length > UniformData[slot].Length)
        Array.Resize(ref UniformData[slot], data.Length);
      data.CopyTo(UniformData[slot]);
    }

    public record struct Sampler(Texture Texture, TextureSampler TextureSampler);
  }
}
