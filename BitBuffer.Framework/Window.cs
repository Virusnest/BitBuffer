using System.Runtime.InteropServices;
using System.Text;

using SDL3;

namespace BitBuffer.Framework
{
  public class Window
  {

    public App app { get; private set; } = null!;
    public nint Handle { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }
    public string Title { get; private set; } = "";
    public bool IsOpen { get; private set; } = true;


    public Window(int width, int height, string title, App app)
    {
      this.app = app;
      Width = width;
      Height = height;
      Title = title;
      Handle = SDL.CreateWindow(title, width, height, SDL.WindowFlags.AlwaysOnTop | SDL.WindowFlags.Resizable);
    }

    public void Dispose()
    {
      throw new NotImplementedException();
    }
    public void Close()
    {
      if (IsOpen)
      {
        SDL.DestroyWindow(Handle);
        IsOpen = false;
      }
    }
    public void Show()
    {
      SDL.ShowWindow(Handle);
    }

    public event Action? OnFocusGain = null;

    /// <summary>
    /// Called when the Window loses focus
    /// </summary>
    public event Action? OnFocusLost = null;

    /// <summary>
    /// Called when the Mouse enters the Window
    /// </summary>
    public event Action? OnMouseEnter = null;

    /// <summary>
    /// Called when the Mouse leaves the Window
    /// </summary>
    public event Action? OnMouseLeave = null;

    /// <summary>
    /// Called when the Window is resized
    /// </summary>
    public event Action? OnResize = null;

    /// <summary>
    /// Called when the Window is restored (after being minimized)
    /// </summary>
    public event Action? OnRestore = null;

    /// <summary>
    /// Called when the Window is maximized
    /// </summary>
    public event Action? OnMaximize = null;

    /// <summary>
    /// Called when the Window is minimized
    /// </summary>
    public event Action? OnMinimize = null;

    /// <summary>
    /// Called when the Window enters full screen mode
    /// </summary>
    public event Action? OnFullscreenEnter = null;

    /// <summary>
    /// Called when the Window exits full screen mode
    /// </summary>
    public event Action? OnFullscreenExit = null;

    /// <summary>
    /// What action(s) to perform when the user requests for the Window to close.
    /// If not assigned, the default behavior will call <see cref="App.Exit"/>.
    /// </summary>
    public Action? OnCloseRequested;

    internal void OnEvent(SDL.EventType ev)
    {
      switch (ev)
      {
        case SDL.EventType.WindowFocusGained:
          OnFocusGain?.Invoke();
          break;
        case SDL.EventType.WindowFocusLost:
          OnFocusLost?.Invoke();
          break;
        case SDL.EventType.WindowMouseEnter:
          OnMouseEnter?.Invoke();
          break;
        case SDL.EventType.WindowMouseLeave:
          OnMouseLeave?.Invoke();
          break;
        case SDL.EventType.WindowResized:
          OnResize?.Invoke();
          break;
        case SDL.EventType.WindowRestored:
          OnRestore?.Invoke();
          break;
        case SDL.EventType.WindowMaximized:
          OnMaximize?.Invoke();
          break;
        case SDL.EventType.WindowMinimized:
          OnMinimize?.Invoke();
          break;
        case SDL.EventType.WindowEnterFullscreen:
          OnFullscreenEnter?.Invoke();
          break;
        case SDL.EventType.WindowLeaveFullscreen:
          OnFullscreenExit?.Invoke();
          break;
        case SDL.EventType.WindowCloseRequested:
          if (OnCloseRequested != null)
            OnCloseRequested.Invoke();
          else
            app.Exit();
          break;
      }
    }
  }
}