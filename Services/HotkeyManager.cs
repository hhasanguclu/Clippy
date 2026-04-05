using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using SharpHook;
using SharpHook.Native;

namespace Clippy.Services;

public class HotkeyManager : IDisposable
{
    private SimpleGlobalHook? _hook;
    private bool _ctrlPressed;
    private bool _shiftPressed;

    public event Action? HotkeyPressed;

    public bool Register()
    {
        try
        {
            _hook = new SimpleGlobalHook();
            _hook.KeyPressed += OnKeyPressed;
            _hook.KeyReleased += OnKeyReleased;
            Task.Run(() => _hook.Run());
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
    {
        switch (e.Data.KeyCode)
        {
            case KeyCode.VcLeftControl:
            case KeyCode.VcRightControl:
                _ctrlPressed = true;
                break;
            case KeyCode.VcLeftShift:
            case KeyCode.VcRightShift:
                _shiftPressed = true;
                break;
            case KeyCode.VcV:
                if (_ctrlPressed && _shiftPressed)
                {
                    HotkeyPressed?.Invoke();
                }
                break;
        }
    }

    private void OnKeyReleased(object? sender, KeyboardHookEventArgs e)
    {
        switch (e.Data.KeyCode)
        {
            case KeyCode.VcLeftControl:
            case KeyCode.VcRightControl:
                _ctrlPressed = false;
                break;
            case KeyCode.VcLeftShift:
            case KeyCode.VcRightShift:
                _shiftPressed = false;
                break;
        }
    }

    public void Dispose()
    {
        _hook?.Dispose();
    }
}
