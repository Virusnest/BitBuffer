using System;

namespace BitBuffer.Framework;

public interface IDisposableObject : IDisposable
{
  public bool IsDisposed { get; }
}
