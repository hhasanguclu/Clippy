using System;
using System.Windows.Forms;

namespace Clippy.Services
{
    public class HotkeyManager : IDisposable
    {
        private const int HOTKEY_ID = 9000;
        private readonly Form _listenerForm;
        private bool _registered;

        public event Action? HotkeyPressed;

        public HotkeyManager()
        {
            _listenerForm = new HotkeyListenerForm(this);
        }

        public bool Register(Keys key = Keys.V, uint modifiers = NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT)
        {
            if (_registered)
                Unregister();

            _registered = NativeMethods.RegisterHotKey(
                _listenerForm.Handle,
                HOTKEY_ID,
                modifiers | NativeMethods.MOD_NOREPEAT,
                (uint)key
            );

            return _registered;
        }

        public void Unregister()
        {
            if (_registered)
            {
                NativeMethods.UnregisterHotKey(_listenerForm.Handle, HOTKEY_ID);
                _registered = false;
            }
        }

        public void Dispose()
        {
            Unregister();
            _listenerForm?.Dispose();
        }

        private class HotkeyListenerForm : Form
        {
            private readonly HotkeyManager _manager;

            public HotkeyListenerForm(HotkeyManager manager)
            {
                _manager = manager;
                ShowInTaskbar = false;
                FormBorderStyle = FormBorderStyle.None;
                Size = new System.Drawing.Size(0, 0);
                Opacity = 0;
                Show();
                Hide();
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == NativeMethods.WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
                {
                    _manager.HotkeyPressed?.Invoke();
                }
                base.WndProc(ref m);
            }
        }
    }
}
