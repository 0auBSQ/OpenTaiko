using Silk.NET.Core.Contexts;
using Silk.NET.GLFW;
using OpenTK.Graphics.Egl; //OpenTKさん ありがとう!
using Silk.NET.Windowing;
using Silk.NET.OpenGLES;

namespace SampleFramework;

public class AngleContext : IGLContext
{
    private nint Display;    
    private nint Context;
    private nint Surface;

    public AngleContext(AnglePlatformType anglePlatformType, IWindow window)
    {
        nint windowHandle;
        nint display;
        if (window.Native.Kind.HasFlag(NativeWindowFlags.Win32))
        {
            windowHandle = window.Native.Win32.Value.Hwnd;
            display = window.Native.Win32.Value.HDC;
        }
        else if (window.Native.Kind.HasFlag(NativeWindowFlags.X11))
        {
            windowHandle = (nint)window.Native.X11.Value.Window;
            // Temporary fix for the segfaults
            // Note than X11 Display number is NOT always 0, it can be 1, 2 and so on for example in cases of user switching
            display = 0;// Egl.GetDisplay(window.Native.X11.Value.Display);
        }
        else if (window.Native.Kind.HasFlag(NativeWindowFlags.Cocoa))
        {
            windowHandle = window.Native.Cocoa.Value;
            display = 0;
        }
        else if (window.Native.Kind.HasFlag(NativeWindowFlags.Wayland))
        {
            windowHandle = window.Native.Wayland.Value.Surface;
            display = window.Native.Wayland.Value.Display;
        }
        else
        {
            throw new Exception("Window not found");
        }
        
        Source = window;

        int platform = 0;
        switch(anglePlatformType)
        {
            case AnglePlatformType.OpenGL:
            platform = Egl.PLATFORM_ANGLE_TYPE_OPENGL_ANGLE;
            break;
            case AnglePlatformType.OpenGLES:
            platform = Egl.PLATFORM_ANGLE_TYPE_OPENGLES_ANGLE;
            break;
            case AnglePlatformType.D3D9:
            platform = Egl.PLATFORM_ANGLE_TYPE_D3D9_ANGLE;
            break;
            case AnglePlatformType.D3D11:
            platform = Egl.PLATFORM_ANGLE_TYPE_D3D11_ANGLE;
            break;
            case AnglePlatformType.Vulkan:
            platform = Egl.PLATFORM_ANGLE_TYPE_VULKAN_ANGLE;
            break;
            case AnglePlatformType.Metal:
            platform = Egl.PLATFORM_ANGLE_TYPE_METAL_ANGLE;
            break;
        }
        int[] platformAttributes = new int[]
        {
            Egl.PLATFORM_ANGLE_TYPE_ANGLE, platform,
            Egl.NONE
        };

        //getEGLNativeDisplay
        Display = Egl.GetPlatformDisplayEXT(Egl.PLATFORM_ANGLE_ANGLE, display, platformAttributes);

        Egl.Initialize(Display, out int major, out int minor);
        Egl.BindAPI(RenderApi.ES);
        
        IntPtr[] configs = new IntPtr[1];
        int[] configAttributes = new int[]
        {
            Egl.RENDERABLE_TYPE, Egl.OPENGL_ES2_BIT,
            Egl.BUFFER_SIZE, 0,
            Egl.NONE
        };
        unsafe 
        {
            Egl.ChooseConfig(Display, configAttributes, configs, configs.Length, out int num_config);
        }

        int[] contextAttributes = new int[]
        {
            //Egl.CONTEXT_CLIENT_VERSION, 2,
            Egl.CONTEXT_MAJOR_VERSION, 2,
            Egl.CONTEXT_MINOR_VERSION, 0,
            Egl.NONE
        };
        Context = Egl.CreateContext(Display, configs[0], 0, contextAttributes);
        
        int[] surfaceAttributes = new int[]
        {
            Egl.NONE
        };

        //Surface = Egl.CreatePlatformWindowSurfaceEXT(Display, configs[0], windowHandle, null);
        Surface = Egl.CreateWindowSurface(Display, configs[0], windowHandle, 0);

        var error1 = Egl.GetError();
    }

    public nint Handle { get; set; }

    public IGLContextSource? Source { get; set; }

    public bool IsCurrent { get; set; } = true;

    public nint GetProcAddress(string proc, int? slot = null)
    {
        nint addr = Egl.GetProcAddress(proc);
        return addr;
    }

    public bool TryGetProcAddress(string proc, out nint addr, int? slot = null)
    {
        addr = Egl.GetProcAddress(proc);
        return addr != 0;
    }

    public void SwapInterval(int interval)
    {
        Egl.SwapInterval(Display, interval);
    }

    public void SwapBuffers()
    {
        Egl.SwapBuffers(Display, Surface);
    }

    public void MakeCurrent()
    {
        Egl.MakeCurrent(Display, Surface, Surface, Context);
    }

    public void Clear()
    {
    }

    public void Dispose()
    {
        Egl.DestroyContext(Display, Context);
        Egl.DestroySurface(Display, Surface);
        Egl.Terminate(Display);
    }
}