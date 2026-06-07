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
            NativeMethods.RECT rect;
            nint sourceHandle;

            if (handle is null or 0)
            {
                var width = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXSCREEN);
                var height = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYSCREEN);
                rect = new NativeMethods.RECT { Left = 0, Top = 0, Right = width, Bottom = height };
                sourceHandle = NativeMethods.GetDesktopWindow();
            }
            else
            {
                if (!NativeMethods.IsWindow((nint)handle.Value))
                    throw new ToolException(ErrorCodes.WindowGone, "Window handle is no longer valid.");

                if (!NativeMethods.GetWindowRect((nint)handle.Value, out rect))
                    throw new ToolException(ErrorCodes.CaptureFailed, "Could not read window bounds.");

                sourceHandle = (nint)handle.Value;
            }

            var srcX = 0;
            var srcY = 0;
            if (handle is null or 0)
            {
                srcX = rect.Left;
                srcY = rect.Top;
            }

            if (region is not null)
            {
                rect.Left += region.X;
                rect.Top += region.Y;
                rect.Right = rect.Left + region.Width;
                rect.Bottom = rect.Top + region.Height;
            }

            var captureWidth = rect.Width;
            var captureHeight = rect.Height;
            if (captureWidth <= 0 || captureHeight <= 0)
                throw new ToolException(ErrorCodes.CaptureFailed, "Capture region has zero size.");

            using var bitmap = CaptureBitmap(sourceHandle, srcX, srcY, captureWidth, captureHeight);
            using var scaled = ApplyScale(bitmap, scale, out var finalWidth, out var finalHeight);

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

    private static Bitmap CaptureBitmap(nint handle, int srcX, int srcY, int width, int height)
    {
        var hdcWindow = NativeMethods.GetDC(handle);
        var hdcMem = NativeMethods.CreateCompatibleDC(hdcWindow);
        var hBitmap = NativeMethods.CreateCompatibleBitmap(hdcWindow, width, height);
        var hOld = NativeMethods.SelectObject(hdcMem, hBitmap);

        try
        {
            if (!NativeMethods.BitBlt(hdcMem, 0, 0, width, height, hdcWindow, srcX, srcY, NativeMethods.SRCCOPY))
                throw new ToolException(ErrorCodes.CaptureFailed, "BitBlt capture failed.");

            return Image.FromHbitmap(hBitmap);
        }
        finally
        {
            NativeMethods.SelectObject(hdcMem, hOld);
            NativeMethods.DeleteObject(hBitmap);
            NativeMethods.DeleteDC(hdcMem);
            NativeMethods.ReleaseDC(handle, hdcWindow);
        }
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
