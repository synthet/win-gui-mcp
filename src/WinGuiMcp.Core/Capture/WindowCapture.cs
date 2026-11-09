using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
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

            using var bitmap = CaptureBitmap(handle, out var originX, out var originY);
            var crop = ResolveRegion(bitmap.Width, bitmap.Height, region);
            using var cropped = ApplyRegion(bitmap, region is null ? null : crop);
            using var scaled = ApplyScale(cropped, scale, out var finalWidth, out var finalHeight);

            using var ms = new MemoryStream();
            scaled.Save(ms, ImageFormat.Png);

            return new CaptureResult
            {
                ImageBytes = ms.ToArray(),
                MimeType = "image/png",
                Width = finalWidth,
                Height = finalHeight,
                OriginX = originX + crop.X,
                OriginY = originY + crop.Y,
                Scale = Math.Round(finalWidth / (double)cropped.Width, 4)
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

    /// <summary>
    /// Captures like <see cref="Capture"/> but writes a PNG to <paramref name="outputPath"/> instead of
    /// returning base64 (large captures overflow MCP tool results). Returns path, size and SHA-256.
    /// </summary>
    public CaptureFileResult CaptureToFile(nint? handle, CaptureRegion? region, double scale, string outputPath)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
            throw new ToolException(ErrorCodes.InvalidArgument, "outputPath is required.");
        try
        {
            if (handle is 0)
                throw new ToolException(ErrorCodes.InvalidArgument, "handle must be a non-zero HWND integer.");

            using var bitmap = CaptureBitmap(handle, out _, out _);
            using var cropped = ApplyRegion(bitmap, region is null ? null : ResolveRegion(bitmap.Width, bitmap.Height, region));
            using var scaled = ApplyScale(cropped, scale, out var finalWidth, out var finalHeight);

            var full = Path.GetFullPath(outputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            using (var ms = new MemoryStream())
            {
                scaled.Save(ms, ImageFormat.Png);
                File.WriteAllBytes(full, ms.ToArray());
                var sha = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(ms.ToArray())).ToLowerInvariant();
                return new CaptureFileResult { Path = full, Width = finalWidth, Height = finalHeight, Sha256 = sha };
            }
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

    private static Bitmap CaptureBitmap(nint? handle, out int originX, out int originY)
    {
        originX = 0;
        originY = 0;
        if (handle is > 0)
        {
            var hwnd = (nint)handle.Value;
            if (!NativeMethods.IsWindow(hwnd))
                throw new ToolException(ErrorCodes.WindowGone, "Window handle is no longer valid.");

            try
            {
                var wgcBitmap = GraphicsCaptureProvider.TryCaptureWindow(hwnd);
                if (wgcBitmap is not null)
                {
                    if (!IsBlankBitmap(wgcBitmap))
                    {
                        // WGC captures the DWM visual bounds, which exclude the invisible resize border.
                        if (NativeMethods.DwmGetWindowAttributeRect(hwnd, NativeMethods.DWMWA_EXTENDED_FRAME_BOUNDS, out var frame, Marshal.SizeOf<NativeMethods.RECT>()) == 0 ||
                            NativeMethods.GetWindowRect(hwnd, out frame))
                        {
                            originX = frame.Left;
                            originY = frame.Top;
                        }
                        return wgcBitmap;
                    }
                    wgcBitmap.Dispose();
                }
            }
            catch
            {
                // Fall back to PrintWindow / Desktop BitBlt when WGC fails.
            }

            var printBitmap = TryPrintWindow(hwnd);
            if (printBitmap is not null)
            {
                if (!IsBlankBitmap(printBitmap))
                {
                    SetOriginFromWindowRect(hwnd, out originX, out originY);
                    return printBitmap;
                }
                printBitmap.Dispose();
            }

            var desktopBitmap = CaptureWindowFromDesktop(hwnd);
            SetOriginFromWindowRect(hwnd, out originX, out originY);
            return desktopBitmap;
        }

        return CaptureFullDesktop();
    }

    private static void SetOriginFromWindowRect(nint hwnd, out int originX, out int originY)
    {
        NativeMethods.GetWindowRect(hwnd, out var rect);
        originX = rect.Left;
        originY = rect.Top;
    }

    private static Bitmap? TryPrintWindow(nint hwnd)
    {
        if (!NativeMethods.GetWindowRect(hwnd, out var rect))
            return null;

        var width = rect.Width;
        var height = rect.Height;
        if (width <= 0 || height <= 0)
            return null;

        var hdcWindow = NativeMethods.GetDC(hwnd);
        if (hdcWindow == 0)
            return null;

        var hdcMem = NativeMethods.CreateCompatibleDC(hdcWindow);
        var hBitmap = NativeMethods.CreateCompatibleBitmap(hdcWindow, width, height);
        var hOld = NativeMethods.SelectObject(hdcMem, hBitmap);

        try
        {
            if (!NativeMethods.PrintWindow(hwnd, hdcMem, NativeMethods.PW_RENDERFULLCONTENT))
                return null;

            return Image.FromHbitmap(hBitmap);
        }
        catch
        {
            return null;
        }
        finally
        {
            NativeMethods.SelectObject(hdcMem, hOld);
            NativeMethods.DeleteObject(hBitmap);
            NativeMethods.DeleteDC(hdcMem);
            NativeMethods.ReleaseDC(hwnd, hdcWindow);
        }
    }

    private static Bitmap CaptureWindowFromDesktop(nint hwnd)
    {
        if (!NativeMethods.GetWindowRect(hwnd, out var rect))
            throw new ToolException(ErrorCodes.CaptureFailed, "Could not read window bounds.");

        return CaptureDesktopRect(rect);
    }

    private static Bitmap CaptureFullDesktop()
    {
        var width = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXSCREEN);
        var height = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYSCREEN);
        var rect = new NativeMethods.RECT { Left = 0, Top = 0, Right = width, Bottom = height };
        return CaptureDesktopRect(rect);
    }

    private static Bitmap CaptureDesktopRect(NativeMethods.RECT rect)
    {
        var captureWidth = rect.Width;
        var captureHeight = rect.Height;
        if (captureWidth <= 0 || captureHeight <= 0)
            throw new ToolException(ErrorCodes.CaptureFailed, "Capture region has zero size.");

        var desktopHandle = NativeMethods.GetDesktopWindow();
        var hdcDesktop = NativeMethods.GetDC(desktopHandle);
        var hdcMem = NativeMethods.CreateCompatibleDC(hdcDesktop);
        var hBitmap = NativeMethods.CreateCompatibleBitmap(hdcDesktop, captureWidth, captureHeight);
        var hOld = NativeMethods.SelectObject(hdcMem, hBitmap);

        try
        {
            if (!NativeMethods.BitBlt(hdcMem, 0, 0, captureWidth, captureHeight, hdcDesktop, rect.Left, rect.Top, NativeMethods.SRCCOPY))
                throw new ToolException(ErrorCodes.CaptureFailed, "BitBlt desktop capture failed.");

            return Image.FromHbitmap(hBitmap);
        }
        finally
        {
            NativeMethods.SelectObject(hdcMem, hOld);
            NativeMethods.DeleteObject(hBitmap);
            NativeMethods.DeleteDC(hdcMem);
            NativeMethods.ReleaseDC(desktopHandle, hdcDesktop);
        }
    }

    public static bool IsBlankBitmap(Bitmap bitmap)
    {
        if (bitmap.Width <= 0 || bitmap.Height <= 0)
            return true;

        var stepX = Math.Max(1, bitmap.Width / 16);
        var stepY = Math.Max(1, bitmap.Height / 16);

        for (var y = stepY / 2; y < bitmap.Height; y += stepY)
        {
            for (var x = stepX / 2; x < bitmap.Width; x += stepX)
            {
                var pixel = bitmap.GetPixel(x, y);
                if (pixel.R != 0 || pixel.G != 0 || pixel.B != 0)
                    return false;
            }
        }

        return true;
    }

    /// <summary>Clamps a requested crop to the source image; returns the full image when no region is given.</summary>
    public static Rectangle ResolveRegion(int sourceWidth, int sourceHeight, CaptureRegion? region)
    {
        if (region is null)
            return new Rectangle(0, 0, sourceWidth, sourceHeight);

        var x = Math.Clamp(region.X, 0, Math.Max(0, sourceWidth - 1));
        var y = Math.Clamp(region.Y, 0, Math.Max(0, sourceHeight - 1));
        var width = Math.Clamp(region.Width, 1, sourceWidth - x);
        var height = Math.Clamp(region.Height, 1, sourceHeight - y);
        return new Rectangle(x, y, width, height);
    }

    private static Bitmap ApplyRegion(Bitmap source, Rectangle? crop)
    {
        if (crop is null)
            return new Bitmap(source);

        var c = crop.Value;
        var cropped = new Bitmap(c.Width, c.Height);
        using var g = Graphics.FromImage(cropped);
        g.DrawImage(source, new Rectangle(0, 0, c.Width, c.Height), c.X, c.Y, c.Width, c.Height, GraphicsUnit.Pixel);
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
    public required byte[] ImageBytes { get; init; }
    public string ImageBase64 => Convert.ToBase64String(ImageBytes);
    public required string MimeType { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }

    /// <summary>Physical screen coordinates of the image's top-left pixel.</summary>
    public required int OriginX { get; init; }
    public required int OriginY { get; init; }

    /// <summary>Image pixels per physical screen pixel.</summary>
    public required double Scale { get; init; }
}

public sealed class CaptureFileResult
{
    public required string Path { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required string Sha256 { get; init; }
}

/// <summary>
/// Screenshot tool payload. The MCP layer sends <see cref="ImageBytes"/> as an image content block
/// and serializes only <see cref="Metadata"/>; in-process callers still get <see cref="ImageBase64"/>.
/// </summary>
public sealed class ScreenshotEnvelope
{
    public bool Ok { get; init; } = true;
    public required string ImageBase64 { get; init; }

    // Not `required`: System.Text.Json rejects required members that are also ignored.
    [JsonIgnore]
    public byte[] ImageBytes { get; init; } = [];

    public required string MimeType { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required ScreenPoint Origin { get; init; }
    public required double Scale { get; init; }

    public object Metadata() => new { ok = Ok, mimeType = MimeType, width = Width, height = Height, origin = Origin, scale = Scale };
}

public sealed record ScreenPoint(int X, int Y);
