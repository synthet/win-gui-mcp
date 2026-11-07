using System.Runtime.InteropServices;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX.Direct3D11;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace WinGuiMcp.Capture;

internal static class GraphicsCaptureInterop
{
    private static readonly Guid GraphicsCaptureItemGuid = new("79C3F95B-31F7-4EC2-A464-632EF5D30760");
    private static readonly Guid IGraphicsCaptureItemInteropGuid = new("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356");

    [ComImport]
    [Guid("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IGraphicsCaptureItemInterop
    {
        IntPtr CreateForWindow(IntPtr window, ref Guid iid);
    }

    [ComImport]
    [Guid("A9B3D012-3DF2-4EE3-B8D1-8695F457D3C1")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IDirect3DDxgiInterfaceAccess
    {
        [PreserveSig]
        int GetInterface(ref Guid iid, out IntPtr p);
    }

    [DllImport("combase.dll", PreserveSig = true)]
    private static extern int RoGetActivationFactory(
        IntPtr activatableClassId,
        ref Guid iid,
        [MarshalAs(UnmanagedType.IUnknown)] out object factory);

    [DllImport("combase.dll")]
    private static extern int WindowsCreateString(
        [MarshalAs(UnmanagedType.LPWStr)] string sourceString,
        int length,
        out IntPtr hString);

    [DllImport("combase.dll")]
    private static extern int WindowsDeleteString(IntPtr hString);

    [DllImport("d3d11.dll", EntryPoint = "CreateDirect3D11DeviceFromDXGIDevice", PreserveSig = true)]
    private static extern int CreateDirect3D11DeviceFromDXGIDevice(IntPtr dxgiDevice, out IntPtr graphicsDevice);

    public static GraphicsCaptureItem? TryCreateItemForWindow(nint hwnd)
    {
        try
        {
            const string className = "Windows.Graphics.Capture.GraphicsCaptureItem";
            var hr = WindowsCreateString(className, className.Length, out var hClassName);
            if (hr < 0)
                return null;

            try
            {
                var interopGuid = IGraphicsCaptureItemInteropGuid;
                hr = RoGetActivationFactory(hClassName, ref interopGuid, out var factory);
                if (hr < 0 || factory is null)
                    return null;

                var interop = (IGraphicsCaptureItemInterop)factory;
                var itemGuid = GraphicsCaptureItemGuid;
                var itemPointer = interop.CreateForWindow(hwnd, ref itemGuid);
                if (itemPointer == IntPtr.Zero)
                    return null;

                try
                {
                    return Marshal.GetObjectForIUnknown(itemPointer) as GraphicsCaptureItem;
                }
                finally
                {
                    Marshal.Release(itemPointer);
                }
            }
            finally
            {
                WindowsDeleteString(hClassName);
            }
        }
        catch
        {
            return null;
        }
    }

    public static IDirect3DDevice CreateWinRtDevice(ID3D11Device d3dDevice)
    {
        using var dxgiDevice = d3dDevice.QueryInterface<IDXGIDevice>();
        var hr = CreateDirect3D11DeviceFromDXGIDevice(dxgiDevice.NativePointer, out var inspectable);
        if (hr < 0)
            throw new InvalidOperationException($"CreateDirect3D11DeviceFromDXGIDevice failed: 0x{hr:X8}");

        try
        {
            return (IDirect3DDevice)Marshal.GetObjectForIUnknown(inspectable);
        }
        finally
        {
            Marshal.Release(inspectable);
        }
    }

    public static ID3D11Texture2D GetTextureFromSurface(IDirect3DSurface surface)
    {
        var access = (IDirect3DDxgiInterfaceAccess)(object)surface;
        var iid = typeof(ID3D11Texture2D).GUID;
        access.GetInterface(ref iid, out var texturePointer);
        if (texturePointer == IntPtr.Zero)
            throw new InvalidOperationException("Could not get ID3D11Texture2D from capture surface.");

        return new ID3D11Texture2D(texturePointer);
    }
}
