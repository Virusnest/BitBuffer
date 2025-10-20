using System;

namespace BitBuffer.Framework.Util.MathUtils;

public struct RectI
{
  public int X;
  public int Y;
  public int Width;
  public int Height;
  
  public RectI(int x, int y, int width, int height) {
    X = x;
    Y = y;
    Width = width;
    Height = height;
  }
}
