using WinGuiMcp.Common;
using WinGuiMcp.Platform;

namespace WinGuiMcp.Action;

public sealed class InputSimulator
{
    public void ClickAt(int x, int y, string button, int clickCount)
    {
        var target = NativeMethods.WindowFromPoint(new NativeMethods.POINT { X = x, Y = y });
        if (target == 0)
            target = NativeMethods.GetForegroundWindow();

        InputBoundary.ThrowIfBlocked(target);
        MoveMouse(x, y);

        var (down, up) = button switch
        {
            "right" => (NativeMethods.MOUSEEVENTF_RIGHTDOWN, NativeMethods.MOUSEEVENTF_RIGHTUP),
            "middle" => (NativeMethods.MOUSEEVENTF_MIDDLEDOWN, NativeMethods.MOUSEEVENTF_MIDDLEUP),
            _ => (NativeMethods.MOUSEEVENTF_LEFTDOWN, NativeMethods.MOUSEEVENTF_LEFTUP)
        };

        for (var i = 0; i < clickCount; i++)
        {
            SendMouse(down);
            SendMouse(up);
        }
    }

    public void TypeUnicode(string text)
    {
        InputBoundary.ThrowIfBlocked(NativeMethods.GetForegroundWindow());

        foreach (var ch in text)
        {
            var down = new NativeMethods.INPUT
            {
                Type = NativeMethods.INPUT_KEYBOARD,
                U = new NativeMethods.InputUnion
                {
                    Keyboard = new NativeMethods.KEYBDINPUT
                    {
                        wScan = ch,
                        dwFlags = NativeMethods.KEYEVENTF_UNICODE
                    }
                }
            };
            var up = down;
            up.U.Keyboard.dwFlags = NativeMethods.KEYEVENTF_UNICODE | NativeMethods.KEYEVENTF_KEYUP;
            SendInputs([down, up]);
        }
    }

    public void SendHotkey(string keys)
    {
        InputBoundary.ThrowIfBlocked(NativeMethods.GetForegroundWindow());

        var parts = keys.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
            throw new ToolException(ErrorCodes.InvalidArgument, "keys must specify at least one key.");

        var modifiers = new List<ushort>();
        ushort? key = null;

        foreach (var part in parts)
        {
            if (TryGetModifier(part, out var mod))
            {
                modifiers.Add(mod);
                continue;
            }

            if (key.HasValue)
                throw new ToolException(ErrorCodes.InvalidArgument, $"Invalid hotkey chord: {keys}");

            key = TryGetKey(part);
            if (!key.HasValue)
                throw new ToolException(ErrorCodes.InvalidArgument, $"Unknown key: {part}");
        }

        if (!key.HasValue)
            throw new ToolException(ErrorCodes.InvalidArgument, $"Invalid hotkey chord: {keys}");

        var inputs = new List<NativeMethods.INPUT>();
        foreach (var mod in modifiers)
            inputs.Add(KeyEvent(mod, keyUp: false));

        inputs.Add(KeyEvent(key.Value, keyUp: false));
        inputs.Add(KeyEvent(key.Value, keyUp: true));

        for (var i = modifiers.Count - 1; i >= 0; i--)
            inputs.Add(KeyEvent(modifiers[i], keyUp: true));

        SendInputs(inputs.ToArray());
    }

    public void SendEnter() => SendHotkey("enter");

    public void SelectAll() => SendHotkey("ctrl+a");

    private static void MoveMouse(int x, int y)
    {
        var screenWidth = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXSCREEN);
        var screenHeight = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYSCREEN);

        var absoluteX = (int)((x * 65535.0) / screenWidth);
        var absoluteY = (int)((y * 65535.0) / screenHeight);

        SendMouse(NativeMethods.MOUSEEVENTF_MOVE | NativeMethods.MOUSEEVENTF_ABSOLUTE, absoluteX, absoluteY);
    }

    private static void SendMouse(uint flags, int dx = 0, int dy = 0)
    {
        var input = new NativeMethods.INPUT
        {
            Type = NativeMethods.INPUT_MOUSE,
            U = new NativeMethods.InputUnion
            {
                Mouse = new NativeMethods.MOUSEINPUT
                {
                    dx = dx,
                    dy = dy,
                    dwFlags = flags
                }
            }
        };
        SendInputs([input]);
    }

    private static NativeMethods.INPUT KeyEvent(ushort vk, bool keyUp) => new()
    {
        Type = NativeMethods.INPUT_KEYBOARD,
        U = new NativeMethods.InputUnion
        {
            Keyboard = new NativeMethods.KEYBDINPUT
            {
                wVk = vk,
                dwFlags = keyUp ? NativeMethods.KEYEVENTF_KEYUP : 0
            }
        }
    };

    private static void SendInputs(NativeMethods.INPUT[] inputs)
    {
        // Re-check the desktop at the injection site. SendInput's return value does not identify UIPI.
        InputBoundary.ThrowIfInteractiveDesktop();

        var sent = NativeMethods.SendInput((uint)inputs.Length, inputs, System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.INPUT>());
        if (sent == 0)
            throw new ToolException(ErrorCodes.InternalError, "SendInput failed.");
    }

    private static bool TryGetModifier(string part, out ushort vk)
    {
        vk = part.ToLowerInvariant() switch
        {
            "ctrl" or "control" => 0x11,
            "alt" => 0x12,
            "shift" => 0x10,
            "win" or "windows" => 0x5B,
            _ => 0
        };
        return vk != 0;
    }

    private static ushort? TryGetKey(string part)
    {
        var normalized = part.ToLowerInvariant();
        return normalized switch
        {
            "enter" or "return" => 0x0D,
            "tab" => 0x09,
            "escape" or "esc" => 0x1B,
            "space" => 0x20,
            "backspace" => 0x08,
            "delete" or "del" => 0x2E,
            "f1" => 0x70,
            "f2" => 0x71,
            "f3" => 0x72,
            "f4" => 0x73,
            "f5" => 0x74,
            "f6" => 0x75,
            "f7" => 0x76,
            "f8" => 0x77,
            "f9" => 0x78,
            "f10" => 0x79,
            "f11" => 0x7A,
            "f12" => 0x7B,
            _ when normalized.Length == 1 => char.ToUpperInvariant(normalized[0]),
            _ => null
        };
    }
}
