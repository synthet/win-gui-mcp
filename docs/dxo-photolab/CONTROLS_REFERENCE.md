# DxO PhotoLab 10: Controls & Data Model Reference

This document provides a technical dictionary of UI elements, UIA patterns, keyboard accelerators, and `.dop` / `.preset` Lua parameters for **DxO PhotoLab 10**.

---

## 1. UI Control Mapping (From Workspace Screenshot)

The following table details the primary interactive controls identified in `Снимок экрана 2026-10-21 202316.png`:

| UI Area | Element Label / Icon | Control Type | Primary UIA Pattern | Fallback / Shortcut |
| :--- | :--- | :--- | :--- | :--- |
| **Top Navigation** | `PhotoLibrary` | `Button` / `TabItem` | `InvokePattern` / `SelectionItemPattern` | `Ctrl + F1` |
| **Top Navigation** | `Customize` | `Button` / `TabItem` | `InvokePattern` / `SelectionItemPattern` | `Ctrl + F2` |
| **Top Toolbar** | `Compare` | `SplitButton` / `Button` | `InvokePattern` | `Ctrl + D` / `Ctrl + T` |
| **Top Toolbar** | `1:1` (100% Zoom) | `Button` | `InvokePattern` | `F4` |
| **Top Toolbar** | Zoom Dropdown (`26%`) | `ComboBox` | `ValuePattern` / `ExpandCollapsePattern` | `Ctrl + +` / `Ctrl + -` |
| **Top Toolbar** | `Reset` | `Button` | `InvokePattern` | — |
| **Top Toolbar** | `Apply preset` | `Button` | `InvokePattern` | `Ctrl + Shift + P` |
| **Right Header** | `Search for corrections...`| `Edit` | `ValuePattern` / `TextPattern` | Click & Type |
| **Right Header** | Sub-palettes (Light, Color, Detail, Geometry, Local) | `RadioButton` / `Button` | `SelectionItemPattern` / `InvokePattern` | Click |
| **Right Palette** | `Exposure` Checkbox | `CheckBox` | `TogglePattern` | Click |
| **Right Palette** | `Exposure` Stepper / Box | `Edit` / `Spinner` | `ValuePattern` | Click & Type |
| **Right Palette** | `Exposure` Slider | `Slider` | `RangeValuePattern` | Focus + Arrow Keys |
| **Right Palette** | `DxO Smart Lighting` Checkbox | `CheckBox` | `TogglePattern` | Click |
| **Right Palette** | `Smart Lighting Mode` (Uniform / Spot) | `RadioButton` / `Button` | `SelectionItemPattern` | Click |
| **Right Palette** | `Smart Lighting Intensity` Slider | `Slider` | `RangeValuePattern` | Focus + Arrow Keys |
| **Right Palette** | `Selective Tone` (Whites/High/Mid/Shadow/Black) | `Slider` (x5) | `RangeValuePattern` | Focus + Arrow Keys |
| **Right Palette** | `DxO ClearView Plus` Checkbox | `CheckBox` | `TogglePattern` | Click |
| **Right Palette** | `ClearView Plus Intensity` Slider | `Slider` | `RangeValuePattern` | Focus + Arrow Keys |
| **Right Palette** | `Contrast` Slider | `Slider` | `RangeValuePattern` | Focus + Arrow Keys |
| **Right Palette** | `Microcontrast` Slider | `Slider` | `RangeValuePattern` | Focus + Arrow Keys |
| **Bottom Bar** | Thumbnail Items (`DSC_0001.NEF`, etc.) | `ListItem` / `Custom` | `SelectionItemPattern` | `Left` / `Right` Arrow |
| **Bottom Bar** | `[Export to disk]` | `Button` | `InvokePattern` | `Ctrl + Alt + P` |
| **Bottom Bar** | Export Settings Gear | `Button` | `InvokePattern` | Click |

---

## 2. Global Keyboard Shortcuts

| Category | Command | Windows Shortcut | Purpose / Effect |
| :--- | :--- | :--- | :--- |
| **Workspace** | Switch to PhotoLibrary | `Ctrl + F1` | Opens DAM / folder tree view |
| **Workspace** | Switch to Customize | `Ctrl + F2` | Opens RAW developer inspector |
| **Workspace** | Toggle Side Panels | `F9` | Hides/reveals left and right docks (Cinema Mode) |
| **View** | Fit to Screen | `F3` | Scales image to viewport |
| **View** | 100% Zoom | `F4` | 1:1 pixel zoom |
| **View** | Zoom In / Zoom Out | `Ctrl + +` / `Ctrl + -` | Steps zoom level |
| **View** | Compare Mode | `Ctrl + D` or `Ctrl + T` | Toggles split / side-by-side view with original |
| **View** | Highlight Clipping | `Ctrl + W` | Shows blown-out highlight warnings |
| **View** | Shadow Clipping | `Ctrl + B` | Shows crushed black warnings |
| **Adjustments**| Copy Corrections | `Ctrl + Shift + C` | Copies current parameter stack |
| **Adjustments**| Paste Corrections | `Ctrl + Shift + V` | Applies copied parameters to selected image(s) |
| **Adjustments**| Reset All Corrections | `Ctrl + Shift + R` | Reverts image to default preset |
| **Export** | Export Selected Images | `Ctrl + Alt + P` | Launches rendering pipeline |
| **Rating** | Star Ratings (0 to 5) | `Ctrl + 0` to `Ctrl + 5` | Assigns star rating |
| **Tagging** | Pick (Green Flag) | `7` or `P` | Marks photo as accepted |
| **Tagging** | Reject (Red Flag) | `9` or `X` | Marks photo as rejected |
| **Tagging** | Untag / Clear Flag | `8` or `U` | Removes pick/reject flag |

---

## 3. Parametric Sidecar (`.dop`) & Preset Schema

DxO PhotoLab sidecars (`<image_name>.<ext>.dop`) and presets (`.preset`) are structured as Lua tables. When sidecars are updated, PhotoLab live-synchronizes parameters without requiring restart.

### 3.1 Representative `.dop` Lua Table Structure

```lua
Sidecar = {
  Date = "2026-10-21T20:23:16.0000000Z",
  Software = "DxO PhotoLab 10",
  Version = "21.2",
  Settings = {
    Version = "21.2",
    Base = {
      -- Exposure & Lighting
      ExposureActive = true,
      ExposureBias = 0.50,               -- Exposure compensation in EV
      ExposureAutoMode = "Manual",
      LightingActive = true,             -- DxO Smart Lighting
      LightingMode = "Uniform",          -- "Uniform" or "SpotWeighted"
      LightingIntensity = 25,            -- 0 to 100

      -- Selective Tone
      SelectiveToneActive = true,
      HighLight = 0,                     -- Highlights (-100 to 100)
      MidTone = 0,                       -- Midtones (-100 to 100)
      Shadow = 0,                        -- Shadows (-100 to 100)
      BlackPoint = 0,                    -- Blacks (-100 to 100)
      WhitePoint = 0,                    -- Whites (-100 to 100)

      -- Contrast & Clarity
      ClearViewActive = true,            -- DxO ClearView Plus
      ClearViewIntensity = 50,           -- 0 to 100
      ContrastActive = true,
      ContrastValue = 0,                 -- -100 to 100
      MicrocontrastValue = 0,            -- -100 to 100
      MicrocontrastAuto = false,

      -- Demosaicing & Denoising (DeepPRIME)
      NoiseActive = true,
      NoiseMethod = "DeepPRIME XD",      -- "Fast", "PRIME", "DeepPRIME", "DeepPRIME XD", "DeepPRIME XD2s"
      NoiseLuminance = 40,
      NoiseChrominance = 100,

      -- Color & White Balance
      ColorMode = "AsShot",
      WhiteBalanceRawActive = true,
      WhiteBalanceRawPreset = "AsShot",
      WhiteBalanceRawTemperature = 5400,

      -- Optical Module Corrections
      DistortionActive = true,
      DistortionType = "Auto",
      VignettingActive = true,
      VignettingType = "Auto",
      ChromaticAberrationActive = true,
      LensSoftnessActive = true
    }
  }
}
```

### 3.2 Key Automation Implications
1. **Determinism**: Modifying the `.dop` file directly bypasses UI latency, slider resolution limits, and DPI coordinate quirks.
2. **Batch Processing**: An agent can generate 100 `.dop` files for a folder of RAW files in milliseconds, select all in PhotoLab (`Ctrl + A`), and dispatch `Ctrl + Alt + P` (`Export to disk`) to let `DxO.PhotoLab.ProcessingCore.exe` execute the render.
