using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Clippy.Models;
using SharpHook;
using SharpHook.Native;

namespace Clippy.Services;

public class PasteService
{
    /// <summary>
    /// Places the entry content onto the clipboard via the given TopLevel.
    /// </summary>
    public async Task CopyToClipboard(TopLevel topLevel, ClipboardEntry entry)
    {
        var clipboard = topLevel.Clipboard;
        if (clipboard == null) return;

        switch (entry.EntryType)
        {
            case ClipboardEntryType.Text:
            case ClipboardEntryType.Html:
                if (!string.IsNullOrEmpty(entry.Content))
                    await clipboard.SetTextAsync(entry.Content);
                break;
            case ClipboardEntryType.Image:
                // For images, copy the path as text (cross-platform limitation)
                if (!string.IsNullOrEmpty(entry.ImagePath))
                    await clipboard.SetTextAsync(entry.Content);
                break;
        }
    }

    /// <summary>
    /// Copies entry to clipboard and simulates Ctrl+V paste.
    /// </summary>
    public async Task CopyAndPaste(TopLevel topLevel, ClipboardEntry entry)
    {
        await CopyToClipboard(topLevel, entry);
        await Task.Delay(100);
        SimulatePaste();
    }

    private void SimulatePaste()
    {
        try
        {
            var simulator = new EventSimulator();

            // Determine the correct modifier key based on OS
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // macOS uses Cmd+V
                simulator.SimulateKeyPress(KeyCode.VcLeftMeta);
                simulator.SimulateKeyPress(KeyCode.VcV);
                simulator.SimulateKeyRelease(KeyCode.VcV);
                simulator.SimulateKeyRelease(KeyCode.VcLeftMeta);
            }
            else
            {
                // Windows/Linux use Ctrl+V
                simulator.SimulateKeyPress(KeyCode.VcLeftControl);
                simulator.SimulateKeyPress(KeyCode.VcV);
                simulator.SimulateKeyRelease(KeyCode.VcV);
                simulator.SimulateKeyRelease(KeyCode.VcLeftControl);
            }
        }
        catch { }
    }
}
