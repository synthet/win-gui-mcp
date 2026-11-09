namespace WinGuiMcp.Tools;

/// <summary>
/// Usage guidance returned to MCP clients in the initialize result. Kept short: clients usually
/// place it in the model's context. Distilled from failures seen in agent transcripts.
/// </summary>
internal static class ServerInstructions
{
    public const string Text = """
        WinGuiMcp drives Windows desktop apps through UI Automation. Work from refs, not pixels, when the app exposes them.

        Loop: list_windows (or launch_app) -> snapshot / find_element -> act by ref -> verify (get_text, snapshot, or screenshot).

        Keyboard input:
        - Always pass handle to type_text, hotkey and press_key. The server focuses the window and verifies it holds the foreground; FocusFailed means nothing was sent.
        - Without handle (or ref), keys go to whatever window is in front, which may be your own terminal. Check `target` in the result.
        - Send one input action at a time and verify; do not batch input calls in parallel.

        Windows:
        - launch_app returns the app's main window. If the title looks wrong, confirm with list_windows before sending input.
        - Popup and context menus may not appear in list_windows; use get_active_modal for dialogs, or a full-screen screenshot.

        Refs:
        - Refs come from the latest snapshot of that window. After moving, resizing, navigating or opening a dialog, take a new snapshot before reusing refs. find_element searches the last snapshot.

        Screenshots:
        - screenshot returns an image block plus origin and scale. Screen point for image pixel (px, py): x = origin.x + round(px / scale), y = origin.y + round(py / scale). Use scale 0.3-0.5 and region* crops to keep images small.
        - GPU-rendered, Electron and custom-drawn apps often expose little UIA. Use keyboard shortcuts plus screenshots, and confirm a point with inspect_at before clicking it.

        wait_for conditions (hyphenated): element-exists, element-enabled, text-visible, element-gone, element-hidden, value-equals, window-active, window-closed, process-exists, file-exists, file-stable. OS-level conditions take handle 0.

        Safety: never open or actuate Windows Settings (ms-settings:), Control Panel or other restricted system apps. Warn the user before destructive UI actions (delete, send, overwrite, uninstall).
        """;
}
