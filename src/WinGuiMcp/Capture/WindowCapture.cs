using System.Drawing;
using System.Drawing.Imaging;
using WinGuiMcp.Common;
using WinGuiMcp.Platform;

namespace WinGuiMcp.Capture;

public sealed class WindowCapture
{
    public CaptureResult Capture(nint? handle, CaptureRegion? region, double scale)
    {
        try
        {
            if (handle is 0)
                throw new ToolException(ErrorCodes.InvalidArgument, "handle must be a non-zero HWND integer.");

            using var bitmap = CaptureBitmap(handle);
            using var cropped = ApplyRegion(bitmap, region);
            using var scaled = ApplyScale(cropped, scale, out var finalWidth, out var finalHeight);

            using var ms = new MemoryStream();
            scaled.Save(ms, ImageFormat.Png);

            return new CaptureResult
            {
                ImageBase64 = Convert.ToBase64String(ms.ToArray()),
                MimeType = "image/png",
                Width = finalWidth,
                Height = finalHeight
            };
        }
        catch (ToolException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ToolException(ErrorCodes.CaptureFailed, $"Screenshot capture failed: {ex.Message}");
        }
    }

    private static Bitmap CaptureBitmap(nint? handle)
    {
        if (handle is > 0)
        {
            if (!NativeMethods.IsWindow((nint)handle.Value))
                throw new ToolException(ErrorCodes.WindowGone, "Window handle is no longer valid.");

            var wgcBitmap = GraphicsCaptureProvider.TryCaptureWindow((nint)handle.Value);
            if (wgcBitmap is not null)
                return wgcBitmap;
        }

        return CaptureWithBitBlt(handle);
    }

    private static Bitmap CaptureWithBitBlt(nint? handle)
    {
        NativeMethods.RECT rect;
        nint sourceHandle;
        var srcX = 0;
        var srcY = 0;

        if (handle is null or 0)
        {
            var width = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXSCREEN);
            var height = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYSCREEN);
            rect = new NativeMethods.RECT { Left = 0, Top = 0, Right = width, Bottom = height };
            sourceHandle = NativeMethods.GetDesktopWindow();
            srcX = rect.Left;
            srcY = rect.Top;
        }
        else
        {
            if (!NativeMethods.GetWindowRect((nint)handle.Value, out rect))
                throw new ToolException(ErrorCodes.CaptureFailed, "Could not read window bounds.");

            sourceHandle = (nint)handle.Value;
        }

        var captureWidth = rect.Width;
        var captureHeight = rect.Height;
        if (captureWidth <= 0 || captureHeight <= 0)
            throw new ToolException(ErrorCodes.CaptureFailed, "Capture region has zero size.");

        var hdcWindow = NativeMethods.GetDC(sourceHandle);
        var hdcMem = NativeMethods.CreateCompatibleDC(hdcWindow);
        var hBitmap = NativeMethods.CreateCompatibleBitmap(hdcWindow, captureWidth, captureHeight);
        var hOld = NativeMethods.SelectObject(hdcMem, hBitmap);

        try
        {
            if (!NativeMethods.BitBlt(hdcMem, 0, 0, captureWidth, captureHeight, hdcWindow, srcX, srcY, NativeMethods.SRCCOPY))
                throw new ToolException(ErrorCodes.CaptureFailed, "BitBlt capture failed.");

            return Image.FromHbitmap(hBitmap);
        }
        finally
        {
            NativeMethods.SelectObject(hdcMem, hOld);
            NativeMethods.DeleteObject(hBitmap);
            NativeMethods.DeleteDC(hdcMem);
            NativeMethods.ReleaseDC(sourceHandle, hdcWindow);
        }
    }

    private static Bitmap ApplyRegion(Bitmap source, CaptureRegion? region)
    {
        if (region is null)
            return new Bitmap(source);

        var x = Math.Clamp(region.X, 0, Math.Max(0, source.Width - 1));
        var y = Math.Clamp(region.Y, 0, Math.Max(0, source.Height - 1));
        var width = Math.Clamp(region.Width, 1, source.Width - x);
        var height = Math.Clamp(region.Height, 1, source.Height - y);

        var cropped = new Bitmap(width, height);
        using var g = Graphics.FromImage(cropped);
        g.DrawImage(source, new Rectangle(0, 0, width, height), x, y, width, height, GraphicsUnit.Pixel);
        return cropped;
    }

    private static Bitmap ApplyScale(Bitmap source, double scale, out int width, out int height)
    {
        if (scale >= 0.999)
        {
            width = source.Width;
            height = source.Height;
            return new Bitmap(source);
        }

        width = Math.Max(1, (int)(source.Width * scale));
        height = Math.Max(1, (int)(source.Height * scale));
        var scaled = new Bitmap(width, height);
        using var g = Graphics.FromImage(scaled);
        g.DrawImage(source, 0, 0, width, height);
        return scaled;
    }
}

public sealed class CaptureRegion
{
    public required int X { get; init; }
    public required int Y { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
}

public sealed class CaptureResult
{
    public required string ImageBase64 { get; init; }
    public required string MimeType { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
}
