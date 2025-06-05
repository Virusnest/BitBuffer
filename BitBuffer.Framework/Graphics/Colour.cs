using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace BitBuffer.Framework.Graphics;

[StructLayout(LayoutKind.Sequential)]
public struct Colour
{
  public float R, G, B, A;

  public Colour(float r, float g, float b, float a)
  {
    R = r;
    G = g;
    B = b;
    A = a;
  }

  public Colour(float r, float g, float b) : this(r, g, b, 1.0f) { } // Default alpha to 1.0
  public Colour(byte r, byte g, byte b, byte a) : this(r / 255f, g / 255f, b / 255f, a / 255f) { } // Convert byte to float
  public Colour(int r, int g, int b, int a) : this(r / 255f, g / 255f, b / 255f, a / 255f) { } // Convert int to float
  public Colour(int c) : this((c >> 16) & 0xFF, (c >> 8) & 0xFF, c & 0xFF, (c >> 24) & 0xFF) { } // Convert int to float
  public static Colour White = new Colour(1.0f, 1.0f, 1.0f, 1.0f);
  public static Colour Black = new Colour(0.0f, 0.0f, 0.0f, 1.0f);
  public static Colour Red = new Colour(1.0f, 0.0f, 0.0f, 1.0f);
  public static Colour Green = new Colour(0.0f, 1.0f, 0.0f, 1.0f);
  public static Colour Blue = new Colour(0.0f, 0.0f, 1.0f, 1.0f);
  public static Colour Yellow = new Colour(1.0f, 1.0f, 0.0f, 1.0f);
  public static Colour Cyan = new Colour(0.0f, 1.0f, 1.0f, 1.0f);
  public static Colour Magenta = new Colour(1.0f, 0.0f, 1.0f, 1.0f);
  public static Colour Orange = new Colour(1.0f, 0.5f, 0.0f, 1.0f);
  public static Colour Purple = new Colour(0.5f, 0.0f, 0.5f, 1.0f);
  public static Colour Pink = new Colour(1.0f, 0.75f, 0.8f, 1.0f);
  public static Colour Grey = new Colour(0.5f, 0.5f, 0.5f, 1.0f);
  public static Colour LightGrey = new Colour(0.75f, 0.75f, 0.75f, 1.0f);
  public static Colour DarkGrey = new Colour(0.25f, 0.25f, 0.25f, 1.0f);
  public static Colour Transparent = new Colour(0.0f, 0.0f, 0.0f, 0.0f); // Fully transparent

  public static implicit operator Colour(Color color)
  {
    return new Colour(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
  }
  public static implicit operator Color(Colour color)
  {
    return Color.FromArgb((int)(color.A * 255), (int)(color.R * 255), (int)(color.G * 255), (int)(color.B * 255));
  }
  public static Colour operator +(Colour c1, Colour c2)
  {
    return new Colour(c1.R + c2.R, c1.G + c2.G, c1.B + c2.B, c1.A + c2.A);
  }
  public static Colour operator -(Colour c1, Colour c2)
  {
    return new Colour(c1.R - c2.R, c1.G - c2.G, c1.B - c2.B, c1.A - c2.A);
  }
  public static Colour operator *(Colour c1, Colour c2)
  {
    return new Colour(c1.R * c2.R, c1.G * c2.G, c1.B * c2.B, c1.A * c2.A);
  }
  public static Colour operator /(Colour c1, Colour c2)
  {
    return new Colour(c1.R / c2.R, c1.G / c2.G, c1.B / c2.B, c1.A / c2.A);
  }
  public static Colour operator *(Colour c, float scalar)
  {
    return new Colour(c.R * scalar, c.G * scalar, c.B * scalar, c.A);
  }
  public static Colour operator /(Colour c, float scalar)
  {
    return new Colour(c.R / scalar, c.G / scalar, c.B / scalar, c.A);
  }
  public static bool operator ==(Colour c1, Colour c2)
  {
    return c1.R == c2.R && c1.G == c2.G && c1.B == c2.B && c1.A == c2.A;
  }
  public static bool operator !=(Colour c1, Colour c2)
  {
    return !(c1 == c2);
  }

  public float Average()
  {
    return (R + G + B) / 3f;
  }
  public int ToInt()
  {
    return (int)(R * 255) << 16 | (int)(G * 255) << 8 | (int)(B * 255) | (int)(A * 255) << 24;
  }
  public override string ToString()
  {
    return $"Colour(R: {R}, G: {G}, B: {B}, A: {A})";
  }
  public static Colour FromHSV(float h, float s, float v, float a = 1.0f)
  {
    // Convert HSV to RGB using the formula
    return new Colour(
      v * (1 - s * MathF.Max(0, MathF.Min(1, MathF.Cos(h)))),
      v * (1 - s * MathF.Max(0, MathF.Min(1, MathF.Sin(h)))),
      v * (1 - s),
      a // Alpha set to 1.0 by default
    );
  }
  public static Colour FromHSL(float h, float s, float l, float a = 1.0f)
  {
    // Convert HSL to RGB using the formula
    return new Colour(
      l + s * MathF.Min(l, 1 - l) * MathF.Cos(h),
      l + s * MathF.Min(l, 1 - l) * MathF.Sin(h),
      l - s * MathF.Min(l, 1 - l),
      a // Alpha set to 1.0 by default
    );
  }
  public static Colour FromCMYK(float c, float m, float y, float k)
  {
    // Convert CMYK to RGB using the formula
    return new Colour(
      (1 - c) * (1 - k),
      (1 - m) * (1 - k),
      (1 - y) * (1 - k),
      1.0f // Alpha set to 1.0 by default
    );
  }
  public static Colour FromOKLAB(float l, float a, float b)
  {
    // Convert CIE-LAB to RGB using the formula
    return new Colour(
      l + a * 0.4f,
      l + b * 0.4f,
      l - a * 0.4f,
      1.0f // Alpha set to 1.0 by default
    );
  }
  public static Colour FromHex(string hex)
  {
    // Convert hex string to RGB using the formula
    if (hex.StartsWith("#"))
    {
      hex = hex.Substring(1);
    }
    return new(Convert.ToInt32(hex, 16));
  }
}
