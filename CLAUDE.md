# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Unity VR physics education project ("vrtest") targeting SteamVR/OpenXR. It simulates physics laboratory experiments for students, with interactive VR mechanics.

**Unity version**: Inferred from packages — Unity 2022.x+  
**Target platform**: PC VR (SteamVR via OpenXR)

## Development Workflow

There is no CLI build system. All compilation, testing, and scene editing happens inside the **Unity Editor**. Open `artemvr.sln` in Visual Studio or Rider for C# editing with IntelliSense.

To run the project: open Unity Hub → open the project folder → press Play in the Editor, or build via `File > Build Settings`.

There are no automated tests — validation is done by running scenes in the Editor.

## Key Packages

- `com.unity.xr.interaction.toolkit` 2.6.5 — XR interaction layer (grab, snap, teleport)
- `com.unity.xr.openxr` 1.14.3 + SteamVR OpenVR package — headset input
- `com.unity.textmeshpro` 3.0.7 — all in-world UI text
- `com.unity.visualscripting` 1.9.4 — visual scripting (used alongside C#)

## Scene Structure

`Assets/Scenes/` contains one scene per lab experiment:
- `lab1.unity` – `lab6.unity` — individual physics labs
- `SampleScene.unity`, `Sandbox.unity` — development/testing scenes

`SceneChanger.cs` handles scene transitions by name or build index. Scenes must be registered in `File > Build Settings` for index-based loading to work.

## Code Architecture

### Custom Scripts (`Assets/Scripts/`)

**InclinedPlaneExperiment** is the central coordinator for the inclined plane lab. It holds references to `ForceMeter`, `MeasuringTape`, the plane `GameObject`, and the block, and computes efficiency (η = W_useful / W_total) each frame, displaying results via TextMeshProUGUI.

**ForceMeter** is a simple value holder with a `UnityEvent<float>` that fires on force change. Set force via `CurrentForce` property or `SetForce()`.

**MeasuringTape** displays a float distance in world-space via `TextMeshPro`. Call `ShowDistance(float)` to update it.

**LevelBuilder** (file is named `LevelBuilder.cs`, class is `LeverBuilder`) procedurally creates a lever with a `HingeJoint`, snap points, and a base at runtime via `BuildLever()`.

**SnapPoint / RemovableWeight** implement a snap-and-release system for placing weights: `SnapPoint.OnTriggerEnter` kinematically snaps any unselected `XRGrabInteractable` Rigidbody; `RemovableWeight.OnGrabbed` calls `SnapPoint.Release()` to detach it on grab.

**CreateManager** spawns a prefab at a `Transform` or in front of the camera. Spawned objects get a `Rigidbody` added if missing.

**SceneChanger** wraps `SceneManager.LoadScene` with a `ByName`/`ByIndex` mode for UI button wiring.

### JusticeScale (`Assets/JusticeScale/`)

A third-party-style balance scale asset with two detection strategies:

- **`Scale`** (abstract) — base class; exposes `TotalWeight` and a `layerMask`.
- **`TriggerScale`** — uses `OnTriggerEnter/Exit` on a `MeshCollider`; accumulates mass as objects enter/exit.
- **`OverlapScale`** — uses `Physics.OverlapCapsule` each frame to detect Rigidbodies above the pan; parents detected objects to an internal container.
- **`ScaleController`** — computes `BalanceNormalized` (0–1) and `WeightDifference` from left/right `Scale` references using smoothed lerp.
- **`ScaleBeamRotation`** — reads `ScaleController.BalanceNormalized` in `FixedUpdate` and applies Z-axis rotation (±`blendRotation` degrees) to the beam transform.
- **`ScaleUI`** — displays `TotalWeight` in kg or pounds via a legacy `UnityEngine.UI.Text`.

### Editor Tools (`Assets/Editor/`)

- **`LaboratoryInclinedPlaneCreator`** — menu item `Tools/Create Inclined Plane Prefabs` that programmatically creates and saves prefabs for the inclined plane lab.
- **`LaboratoryPrefabCreator`** — similar editor utility for other lab prefabs.

## Conventions

- Scripts are in Russian locale (comments, `Debug.Log` strings, and some UI text are in Russian/Cyrillic). This is intentional.
- XR interaction uses `XRGrabInteractable` from XR Interaction Toolkit; always add listeners in `Start()` and remove them in `OnDestroy()` if the component may be destroyed.
- `FindObjectOfType<T>()` is used for loose coupling between components (e.g., `ExperimentBlock` → `InclinedPlaneExperiment`). Prefer direct Inspector references for new code to avoid runtime search overhead.
- The `JusticeScale` namespace (`JusticeScale.Scripts.*`) is separate from project scripts; keep them isolated.
