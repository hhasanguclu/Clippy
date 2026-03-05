using System;
using System.Threading;
using System.Windows.Forms;

namespace Clippy
{
    static class Program
    {
        private static Mutex? _mutex;

        [STAThread]
        static void Main()
        {
            // Single instance check
            const string mutexName = "Clippy_ClipboardManager_SingleInstance";
            _mutex = new Mutex(true, mutexName, out bool createdNew);

            if (!createdNew)
            {
                // Another instance is already running
                MessageBox.Show(
                    "Clippy is already running.",
                    "Clippy",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

            Application.Run(new TrayContext());
        }
    }
}