# DxO PhotoLab 10 Automation & Control

This directory contains specifications, implementation plans, and reference documentation for controlling and automating **DxO PhotoLab 10** through **WinGuiMcp** (UI Automation / FlaUI UIA3) and parametric sidecar scripting.

---

## Document Index

1. [**`SPEC.md`**](SPEC.md) — Feature and system specification for DxO PhotoLab 10 automation (modes, control surfaces, UIA patterns, sidecar synchronization, acceptance criteria).
2. [**`PLAN.md`**](PLAN.md) — Phased implementation and execution plan (UIA element mapping, recipe development, sidecar integration, and automated verification).
3. [**`CONTROLS_REFERENCE.md`**](CONTROLS_REFERENCE.md) — Exhaustive control hierarchy mapping from the workspace UI, verified keyboard shortcuts, UIA pattern mapping, and `.dop` Lua parameter schema.
4. [**`RESEARCH.md`**](RESEARCH.md) — Exhaustive deep technical research report on DxO PhotoLab 10 plugins, APIs, MCP, community experiments (`DopCor`, `DxO-Batch-Image`), EULA boundaries, and automation tiers.

---

## Executive Summary

DxO PhotoLab 10 is a non-destructive RAW photo editor built on:
- **Presentation Layer**: Windows Presentation Foundation (WPF) with MVVM (`CommunityToolkit.Mvvm`).
- **Render Viewport**: Direct2D / Direct3D 11 (`SharpDX`) / DirectML hardware acceleration for previewing RAW frames.
- **Parametric Storage**: Human-readable Lua table sidecars (`<filename>.<ext>.dop`) and `.preset` files.
- **Processing Engine**: Multithreaded background process `DxO.PhotoLab.ProcessingCore.exe` utilizing ONNX Runtime for DeepPRIME / DeepPRIME XD AI demosaicing and denoising.

Because DxO PhotoLab lacks an official public CLI or REST API, automation is achieved via a **hybrid control architecture**:
- **WinGuiMcp (UIA3 / FlaUI)**: Direct control of WPF controls (tabs, sliders, buttons, filmstrip, export).
- **Sidecar Scripting (`.dop`)**: High-throughput batch parameter generation written directly to Lua sidecar files before triggering batch processing.
