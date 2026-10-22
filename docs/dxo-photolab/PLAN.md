# Implementation Plan: DxO PhotoLab 10 Automation

This plan outlines the staged technical execution for implementing, testing, and validating end-to-end automation of **DxO PhotoLab 10** using **WinGuiMcp** and parametric scripting.

---

## 1. Goal

Provide a robust, deterministic interface for AI agents and test harnesses to:
1. Launch and orchestrate DxO PhotoLab 10.
2. Inspect the UI tree using FlaUI (UIA3) and extract structured snapshots with stable refs.
3. Manipulate adjustment palettes, sliders, steppers, and filmstrip selections.
4. Trigger exports and monitor background processing completion.
5. Support hybrid workflows via programmatic `.dop` Lua sidecar generation.

---

## 2. Touchpoints & Target Components

| Layer | Component | Description |
| :--- | :--- | :--- |
| **WinGuiMcp Host** | `src/WinGuiMcp/` | Standard Phase 1 tools (`launch_app`, `focus_window`, `snapshot`, `click`, `set_value`, `hotkey`, `wait_for`). |
| **Integration Test Suite** | `tests/WinGuiMcp.Tests/GateTests/` | Dedicated gate test fixture verifying multi-step interactions on DxO PhotoLab. |
| **Sidecar Helper** | `docs/dxo-photolab/scripts/` | Optional lightweight helper utility for parsing and generating Lua `.dop` sidecars. |
| **Documentation** | `docs/dxo-photolab/` | Specification, plan, and control reference tables. |

---

## 3. Phased Implementation Approach

### Phase 1: UIA Discovery & Control Mapping (Discovery Phase)
1. **Target Inspection**:
   - Run DxO PhotoLab with a sample folder of RAW images (e.g., Nikon `.NEF` or Sony `.ARW`).
   - Use `WinGuiMcp` `snapshot` tool and FlaUI UIA3 inspection to capture the live accessibility tree of:
     - Top navigation bar (`PhotoLibrary`, `Customize`).
     - Right adjustment palettes (Exposure, Smart Lighting, Selective Tone, ClearView Plus, Contrast).
     - Bottom filmstrip items and `[Export to disk]` button.
2. **Catalog Automation IDs**:
   - Record exact `AutomationId`, `Name`, `ClassName`, and supported patterns (`RangeValue`, `Value`, `Invoke`, `Toggle`) into [`CONTROLS_REFERENCE.md`](CONTROLS_REFERENCE.md).

### Phase 2: Core UI Interaction Recipes (WinGuiMcp Workflows)
1. **Workspace Navigation Recipe**:
   - Verify deterministic switching between `PhotoLibrary` and `Customize` using both `click` on tab buttons and `hotkey` (`Ctrl+F1` / `Ctrl+F2`).
2. **Adjustment Tool Manipulation**:
   - Validate setting Exposure value via:
     - Direct numeric input on stepper: `set_value` -> `+0.75`.
     - Slider manipulation via `RangeValuePattern`.
   - Validate toggling palette checkboxes on and off.
3. **Filmstrip Selection**:
   - Implement selection of thumbnail `i` using element selection patterns or keyboard arrow navigation.

### Phase 3: Export Pipeline & Process Synchronization
1. **Export Activation**:
   - Automate click on `[Export to disk]` or dispatch `hotkey` `Ctrl+Alt+P`.
2. **Completion Monitoring**:
   - Implement a polling/wait condition checking for:
     - Absence of busy progress bars in UI.
     - Termination of rendering activity in `DxO.PhotoLab.ProcessingCore.exe`.
     - Existence and non-zero size of the destination JPEG/DNG file.

### Phase 4: Hybrid Sidecar Automation (`.dop` Generator)
1. **Sidecar Schema Definition**:
   - Document the essential Lua table keys for high-value operations:
     - `ExposureCompensation`
     - `ColorMode` / `ColorRendering`
     - `NoiseActive` + `NoiseMethod = "DeepPRIME XD"`
     - `ClearViewIntensity`
2. **Sidecar Sync Recipe**:
   - Write `.dop` sidecar to disk before PhotoLab loads the folder or trigger a folder reload.
   - Verify that PhotoLab automatically adopts the sidecar settings into the UI.

---

## 4. Contracts & Tool Schemas

This automation adheres to the existing canonical tool contracts defined in [`docs/tools.md`](../tools.md):

- `launch_app(path, args)` -> Process PID and window handle.
- `focus_window(title)` -> Focus confirmation.
- `snapshot(windowId)` -> Structured accessibility tree with element refs (`w1e42`).
- `set_value(ref, value)` -> Direct parameter input on steppers/sliders.
- `click(ref)` -> Pattern-first invocation of buttons/tabs.
- `hotkey(keys)` -> Dispatching accelerators (`Ctrl+Alt+P`, `F3`, `F4`, `Ctrl+F2`).
- `wait_for(ref, condition, timeoutMs)` -> Synchronization against export spinners or notifications.

---

## 5. Verification & Test Plan

### Automated Test Cases (`tests/WinGuiMcp.Tests/GateTests/DxoPhotoLabGateTests.cs`):
1. **`Test_PhotoLab_LaunchAndModeSwitch`**:
   - Launch `DxO.PhotoLab.exe`.
   - Ensure window appears within 10,000 ms.
   - Toggle between `PhotoLibrary` and `Customize` tabs; assert active tab state changes.
2. **`Test_PhotoLab_AdjustExposureAndVerify`**:
   - In `Customize` mode, locate the Exposure input.
   - Set value to `1.25`.
   - Read property back via `get_properties`; assert value equals `1.25` or `+1.25`.
3. **`Test_PhotoLab_ExportSingleImage`**:
   - Select active image.
   - Trigger `[Export to disk]`.
   - Verify export job spawns `DxO.PhotoLab.ProcessingCore.exe` and completes without error.

### Manual Verification Checklist:
- [ ] DPI scaling verification on multi-monitor high-DPI setups (100%, 150%, 200%).
- [ ] Verify behavior when trial expiration / buy now banner is present.
- [ ] Confirm sidecar file `.dop` integrity after modifying settings in UI vs disk.

---

## 6. Risks & Mitigations

| Risk | Impact | Mitigation |
| :--- | :--- | :--- |
| **Complex Sub-Palettes Collapsed** | UIA cannot find controls that are scrolled out of view or collapsed. | Ensure parent palette tab (e.g. Light icon `☀`) is activated and scroll container into view before querying descendants. |
| **Modal Trial / Welcome Dialogs** | Blocks automation from accessing main window controls. | Include a pre-flight window check for modal dialogs and dismiss with `Escape` or `Enter`. |
| **High GPU / DirectML Load** | Export renders can take seconds or minutes depending on GPU. | Use generous timeouts with periodic file-watch checks instead of fixed sleeps. |
