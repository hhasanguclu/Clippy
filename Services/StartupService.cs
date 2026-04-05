using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;

namespace Clippy.Services;

[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
public static class StartupService
{
    private const string AppName = "Clippy";

    public static bool IsStartupEnabled()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return IsStartupEnabledWindows();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return IsStartupEnabledLinux();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return IsStartupEnabledMacOS();
        return false;
    }

    public static void SetStartup(bool enable)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            SetStartupWindows(enable);
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            SetStartupLinux(enable);
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            SetStartupMacOS(enable);
    }

    // --- Windows ---
    private static bool IsStartupEnabledWindows()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);
            return key?.GetValue(AppName) != null;
        }
        catch { return false; }
    }

    private static void SetStartupWindows(bool enable)
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            if (key == null) return;

            if (enable)
            {
                var exePath = Environment.ProcessPath
                    ?? System.Reflection.Assembly.GetExecutingAssembly().Location;
                key.SetValue(AppName, $"\"{exePath}\"");
            }
            else
            {
                key.DeleteValue(AppName, false);
            }
        }
        catch { }
    }

    // --- Linux (XDG Autostart) ---
    private static string LinuxDesktopFilePath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".config", "autostart", $"{AppName}.desktop");

    private static bool IsStartupEnabledLinux()
    {
        return File.Exists(LinuxDesktopFilePath);
    }

    private static void SetStartupLinux(bool enable)
    {
        try
        {
            if (enable)
            {
                var dir = Path.GetDirectoryName(LinuxDesktopFilePath)!;
                Directory.CreateDirectory(dir);
                var exePath = Environment.ProcessPath
                    ?? System.Reflection.Assembly.GetExecutingAssembly().Location;
                var content = $"""
                    [Desktop Entry]
                    Type=Application
                    Name={AppName}
                    Exec={exePath}
                    X-GNOME-Autostart-enabled=true
                    """;
                File.WriteAllText(LinuxDesktopFilePath, content);
            }
            else
            {
                if (File.Exists(LinuxDesktopFilePath))
                    File.Delete(LinuxDesktopFilePath);
            }
        }
        catch { }
    }

    // --- macOS (LaunchAgent) ---
    private static string MacOSPlistPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Library", "LaunchAgents", $"com.clippy.app.plist");

    private static bool IsStartupEnabledMacOS()
    {
        return File.Exists(MacOSPlistPath);
    }

    private static void SetStartupMacOS(bool enable)
    {
        try
        {
            if (enable)
            {
                var dir = Path.GetDirectoryName(MacOSPlistPath)!;
                Directory.CreateDirectory(dir);
                var exePath = Environment.ProcessPath
                    ?? System.Reflection.Assembly.GetExecutingAssembly().Location;
                var content = $"""
                    <?xml version="1.0" encoding="UTF-8"?>
                    <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
                    <plist version="1.0">
                    <dict>
                        <key>Label</key>
                        <string>com.clippy.app</string>
                        <key>ProgramArguments</key>
                        <array>
                            <string>{exePath}</string>
                        </array>
                        <key>RunAtLoad</key>
                        <true/>
                    </dict>
                    </plist>
                    """;
                File.WriteAllText(MacOSPlistPath, content);
            }
            else
            {
                if (File.Exists(MacOSPlistPath))
                    File.Delete(MacOSPlistPath);
            }
        }
        catch { }
    }
}
