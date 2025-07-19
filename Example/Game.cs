using System;
using BitBuffer.Framework;
using BitBuffer.Framework.Graphics;

namespace Example;

public class Game : App
{
    public Game() : base(new AppConfig(800, 600, "Window"))
    {

    }

    public Shader? Shader;
    public override void Render()
    {
    }

    public override void Init()
    {
        Shader = new Shader(GraphicsState, new(
    @"Texture2D<float4> Texture : register(t0, space2);
SamplerState Sampler : register(s0, space2);

struct Input
{
    float3 Position : TEXCOORD0;
    float2 TexCoord : TEXCOORD1;
};

struct Output
{
    float2 TexCoord : TEXCOORD0;
    float4 Position : SV_Position;
};

Output mainVertex(Input input)
{
    Output output;
    output.TexCoord = input.TexCoord;
    output.Position = float4(input.Position, 1.0f);
    return output;
}

float4 mainFragment(float2 TexCoord : TEXCOORD0) : SV_Target0
{
    return Texture.Sample(Sampler, TexCoord);
}" + "\0"));
    }
    public override void Update()
    {
    }
}
