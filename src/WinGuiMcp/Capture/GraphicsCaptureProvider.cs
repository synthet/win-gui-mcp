using System.Drawing;
using System.Drawing.Imaging;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace WinGuiMcp.Capture;

internal static class GraphicsCaptureProvider
{
    private const int CaptureTimeoutMs = 3000;

    public static Bitmap? TryCaptureWindow(nint hwnd)
    {
        if (hwnd == 0)
            return null;

        var item = GraphicsCaptureInterop.TryCreateItemForWindow(hwnd);
        if (item is null)
            return null;

        using var d3dDevice = D3D11.D3D11CreateDevice(DriverType.Hardware, DeviceCreationFlags.BgraSupport);
        using var winRtDevice = GraphicsCaptureInterop.CreateWinRtDevice(d3dDevice);

        var size = item.Size;
        if (size.Width <= 0 || size.Height <= 0)
            return null;

        using var framePool = Direct3D11CaptureFramePool.CreateFreeThreaded(
            winRtDevice,
            DirectXPixelFormat.B8G8R8A8UIntNormalized,
            1,
            size);

        using var session = framePool.CreateCaptureSession(item);
        using var frameReady = new ManualResetEventSlim(false);
        Direct3D11CaptureFrame? capturedFrame = null;

        framePool.FrameArrived += (_, _) => frameReady.Set();
        session.StartCapture();

        if (!frameReady.Wait(CaptureTimeoutMs))
            return null;

        capturedFrame = framePool.TryGetNextFrame();
        if (capturedFrame is null)
            return null;

        using (capturedFrame)
        {
            using var surfaceTexture = GraphicsCaptureInterop.GetTextureFromSurface(capturedFrame.Surface);
            return CopyTextureToBitmap(d3dDevice, surfaceTexture, size.Width, size.Height);
        }
    }

    private static Bitmap CopyTextureToBitmap(ID3D11Device device, ID3D11Texture2D source, int width, int height)
    {
        var description = source.Description;
        description.BindFlags = BindFlags.None;
        description.CPUAccessFlags = CpuAccessFlags.Read;
        description.Usage = ResourceUsage.Staging;
        description.MiscFlags = ResourceOptionFlags.None;

        using var staging = device.CreateTexture2D(description);
        device.ImmediateContext.CopyResource(staging, source);

        var mapped = device.ImmediateContext.Map(staging, 0, MapMode.Read, Vortice.Direct3D11.MapFlags.None);
        try
        {
            var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            var bounds = new Rectangle(0, 0, width, height);
            var bitmapData = bitmap.LockBits(bounds, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            try
            {
                var sourcePtr = mapped.DataPointer;
                var destPtr = bitmapData.Scan0;
                var rowBytes = width * 4;
                for (var y = 0; y < height; y++)
                {
                    unsafe
                    {
                        Buffer.MemoryCopy(
                            (void*)(sourcePtr + y * mapped.RowPitch),
                            (void*)(destPtr + y * bitmapData.Stride),
                            rowBytes,
                            rowBytes);
                    }
                }
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }

            return bitmap;
        }
        finally
        {
            device.ImmediateContext.Unmap(staging, 0);
        }
    }
}
