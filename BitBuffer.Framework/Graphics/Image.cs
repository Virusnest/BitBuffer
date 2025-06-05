using System.Drawing;
using StbImageSharp;

namespace TileGame.Graphics;

public class Image
{
  public uint DataWidth;

  public Image(uint width, uint height)
  {
    Width = width;
    Height = height;
    Data = new byte[width * height * 4];
  }

  public Image(uint width, uint height, byte[] data)
  {
    Width = width;
    Height = height;
    Data = data;
  }

  public uint Width { get; set; }
  public uint Height { get; set; }

  public byte[] Data { get; set; }

  //create a checkerboard pattern of black and magenta
  public static Image MissingTexture
  {
    get
    {
      var img = new Image(8, 8);
      for (uint i = 0; i < 8; i++)
        for (uint j = 0; j < 8; j++)
        {
          var colorIndex = (i / 4 + j / 4) % 2;
          img.SetPixel(i, j, colorIndex == 1 ? Color.Black : Color.Magenta);
        }

      return img;
    }
  }

  public static Image LoadFromFile(string path)
  {
    StbImage.stbi_set_flip_vertically_on_load(0);
    // Load the image.
    var image = ImageResult.FromStream(File.OpenRead(path), ColorComponents.RedGreenBlueAlpha);
    return new Image((uint)image.Width, (uint)image.Height, image.Data);
  }

  public void SetData(byte[] data)
  {
    Data = data;
  }

  public void SetData(uint i, byte data)
  {
    Data[i] = data;
  }

  public void SetPixel(uint x, uint y, float r, float g, float b, float a)
  {
    var index = (y * Width + x) * 4;
    Data[index] = (byte)(r * 255);
    Data[index + 1] = (byte)(g * 255);
    Data[index + 2] = (byte)(b * 255);
    Data[index + 3] = (byte)(a * 255);
  }

  public void SetPixel(uint x, uint y, Color color)
  {
    SetPixel(x, y, color.R, color.G, color.B, 1);
  }

  public byte GetData(uint i)
  {
    return Data[i];
  }

  //set sub data
  public void SetSubData(int x, int y, uint width, uint height, byte[] data)
  {
    // data is made up of 4 bytes per pixel
    var bytesPerRow = width * 4;
    for (var row = 0; row < height; row++)
    {
      // Calculate the source start index in the data array
      var sourceStartIndex = row * bytesPerRow;

      // Calculate the destination start index in the texture array
      var destinationStartIndex = ((y + row) * Width + x) * 4;
      //Console.WriteLine(Data.Length);
      //Console.WriteLine(data.Length);

      // Copy the row data from source to destination
      Array.Copy(data, sourceStartIndex, Data, destinationStartIndex, bytesPerRow);
    }
  }
}