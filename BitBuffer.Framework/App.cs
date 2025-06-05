using System;
using BitBuffer.Framework.Graphics;
using BitBuffer.Framework.Util;
using SDL3;

namespace BitBuffer.Framework;


public readonly record struct AppConfig
{

  public readonly int Width;
  public readonly int Height;
  public readonly string Title;
  public readonly UpdateMode UpdateMode;
  public readonly float FixedUpdatePeriod;


  public AppConfig(int width, int height, string title, UpdateMode updateMode = UpdateMode.FixedUpdate, float fixedUpdatePeriod = 1f / 60f)
  {
    Width = width;
    Height = height;
    Title = title;
    UpdateMode = updateMode;
    FixedUpdatePeriod = fixedUpdatePeriod;
  }
};
public abstract class App
{
  public GraphicsState GraphicsState = new GraphicsStateSDL();

  public Window Window { get; set; } = null!;
  public readonly AppConfig Config;
  public App(string name, int width, int height)
  : this(new(width, height, name)) { }

  public App(in AppConfig config)
  {
    Config = config;
    Window = new Window(config.Width, config.Height, config.Title, this);
  }
  public void Run()
  {
    SDL.Init(SDL.InitFlags.Video | SDL.InitFlags.Events | SDL.InitFlags.Joystick | SDL.InitFlags.Gamepad);
    Window.Show();
    GraphicsState.Initialize(Window);
    Init();
    PollEvents();
    while (true)
    {
      if (!Window.IsOpen)
      {
        break;
      }
      PollEvents();
      Update();
      Render();
      GraphicsState.Present();
    }
  }
  public abstract void Update();
  public abstract void Render();
  public abstract void Init();

  public void Exit()
  {
    Window.Close();
  }

  private void PollEvents()
  {
    // we shouldn't need to pump events here, but we've found that if we don't,
    // there are issues on MacOS with it not receiving mouse clicks correctly
    SDL.PumpEvents();

    while (SDL.PollEvent(out var ev) && ev.Type != (uint)SDL.EventType.PollSentinel)
    {
      switch ((SDL.EventType)ev.Type)
      {
        case SDL.EventType.Quit:

          Exit();

          break;

        // input
        case SDL.EventType.MouseButtonDown:
        case SDL.EventType.MouseButtonUp:
        case SDL.EventType.MouseWheel:
        case SDL.EventType.KeyDown:
        case SDL.EventType.KeyUp:
        case SDL.EventType.TextInput:
        case SDL.EventType.JoystickAdded:
        case SDL.EventType.JoystickRemoved:
        case SDL.EventType.JoystickButtonDown:
        case SDL.EventType.JoystickButtonUp:
        case SDL.EventType.JoystickAxisMotion:
        case SDL.EventType.GamepadAdded:
        case SDL.EventType.GamepadRemoved:
        case SDL.EventType.GamepadButtonDown:
        case SDL.EventType.GamepadButtonUp:
        case SDL.EventType.GamepadAxisMotion:
          //inputProvider.OnEvent(ev);
          break;

        case SDL.EventType.WindowFocusGained:
        case SDL.EventType.WindowFocusLost:
        case SDL.EventType.WindowMouseEnter:
        case SDL.EventType.WindowMouseLeave:
        case SDL.EventType.WindowResized:
        case SDL.EventType.WindowRestored:
        case SDL.EventType.WindowMaximized:
        case SDL.EventType.WindowMinimized:
        case SDL.EventType.WindowEnterFullscreen:
        case SDL.EventType.WindowLeaveFullscreen:
        case SDL.EventType.WindowCloseRequested:
          if (ev.Window.WindowID == Window.Handle)
            Window.OnEvent((SDL.EventType)ev.Type);
          break;

        default:
          break;
      }
    }
  }


}
