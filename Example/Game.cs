using System;
using System.Numerics;
using BitBuffer.Framework;
using BitBuffer.Framework.Graphics;
using BitBuffer.Framework.Util.MathUtils;

namespace Example;

public class Game : App
{
    public Game() : base(new AppConfig(800, 600, "Window"))
    {

    }

    public Shader? Shader;
    VertexBuffer<vertex> vertexBuffer;
    VertexLayout vertexLayout;
    public override void Render()
    {
        var drawCommand = new DrawCommand();
        drawCommand.VertexBuffers.Add(vertexBuffer);
        drawCommand.InstanceInputRates.Add(false);
        var material = new Material();
        material.Shader = Shader;
        drawCommand.Viewport = new RectI(0,0,Window.Width, Window.Height);
        drawCommand.Material = material;
        drawCommand.VertexCount = 3;

        GraphicsState.PerformDraw(drawCommand);
    }

    public override void Init()
    {
        Shader = new Shader(GraphicsState, new(
    @"
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
    return float4(1.0f, 1.0f, 1.0f, 1.0f);
}
"));
        vertexLayout = VertexLayout.CreateLayout<vertex>(
            new VertexLayout.VertexAttribute(0, /*components ignored*/ 0, VertexType.Float3, false),
            new VertexLayout.VertexAttribute(1, /*components ignored*/ 0, VertexType.Float3, false)
        );

        vertexBuffer = new VertexBuffer<vertex>(GraphicsState,vertexLayout);
        vertex[] vertices =
        {
            new vertex { Position = new Vector3( 0.0f,  0.5f, 0.0f), TexCoord = new Vector2(0.5f, 0.0f) }, // top
            new vertex { Position = new Vector3(-0.5f, -0.5f, 0.0f), TexCoord = new Vector2(0.0f, 1.0f) }, // bottom left
            new vertex { Position = new Vector3( 0.5f, -0.5f, 0.0f), TexCoord = new Vector2(1.0f, 1.0f) }, // bottom right
        };

        vertexBuffer.Upload(vertices.AsSpan());

    }
    private struct vertex
    {
        public Vector3 Position;
        public Vector2 TexCoord;
    }
    public override void Update()
    {
    }
}
