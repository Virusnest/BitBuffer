using System;

namespace BitBuffer.Framework.Graphics;

public interface IGraphicsDisposable : IDisposable
{
  public bool IsDisposed { get; }
}
