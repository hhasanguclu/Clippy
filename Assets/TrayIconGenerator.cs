using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;

namespace Clippy.Assets;

/// <summary>
/// Generates the tray icon programmatically so no external file is needed.
/// </summary>
public static class TrayIconGenerator
{
    public static WindowIcon CreateClipboardIcon()
    {
        const int size = 64;
        var bitmap = new RenderTargetBitmap(new PixelSize(size, size), new Vector(96, 96));

        using (var ctx = bitmap.CreateDrawingContext())
        {
            // Clipboard body
            var bodyBrush = new SolidColorBrush(Color.FromRgb(0, 122, 204));
            ctx.DrawRectangle(bodyBrush, null, new Avalonia.Rect(8, 14, 48, 42), 6, 6);

            // Clipboard clip (top)
            var clipPen = new Pen(new SolidColorBrush(Color.FromRgb(0, 90, 158)), 3);
            ctx.DrawRectangle(null, clipPen, new Avalonia.Rect(20, 4, 24, 16));

            // Lines on clipboard
            var linePen = new Pen(Brushes.White, 2.5);
            ctx.DrawLine(linePen, new Point(18, 30), new Point(46, 30));
            ctx.DrawLine(linePen, new Point(18, 38), new Point(46, 38));
            ctx.DrawLine(linePen, new Point(18, 46), new Point(36, 46));
        }

        // Convert to WindowIcon via PNG stream
        using var ms = new MemoryStream();
        bitmap.Save(ms);
        ms.Position = 0;
        return new WindowIcon(ms);
    }
}
