# WHAT IS UP THERE

> A high-mobility 3D physics-based first-person platformer and hazard survival game developed with **Unity 6** and **C#** for the **BZI Game Jam**.

---

## 🎮 Overview

**WHAT IS UP THERE** is a fast-paced first-person parkour and psychological horror platformer. Players must scale a mysterious vertical environment using advanced momentum-based locomotion, dynamic wall-running, and an elastodynamic grappling hook—all while outmaneuvering synchronized watcher hazards and relentless pursuers lurking in the shadows.

- **Developer:** Reinbrandt Malikiyano Cahyono ([@reinh20](https://github.com/reinh20))
- **Engine:** Unity 6 (`6000.6.5f1`)
- **Render Pipeline:** Universal Render Pipeline (URP)
- **Input System:** Unity Universal Input System (New Input System)
- **Language:** C# (.NET)

---

## ⚙️ Key Technical Features & Software Architecture

### 1. Physics-Driven Kinematic Locomotion (`PlayerMovement.cs`)
* Custom 3D Rigidbody controller simulating true momentum preservation.
* Dynamic counter-movement physics and slope angle detection to eliminate unwanted sliding on inclines.
* Crouching and high-speed sliding mechanics that convert downward kinetic energy into forward velocity.
* Velocity-dependent procedural wind audio and per-step footstep audio systems.

### 2. Surface-Normal Wall Running & Wall Jumping (`WallRun.cs`)
* Algorithmic wall detection utilizing bidirectional raycasting and normal filtering ($y \approx 0$).
* Surface projection using vector cross products: $\vec{v}_{\text{forward}} = \vec{n}_{\text{wall}} \times \vec{u}_{\text{up}}$.
* Directional wall-jump impulses calculated as a weighted sum of upward thrust, wall-normal ejection force, and forward momentum.
* Procedural camera tilt interpolation (`Mathf.Lerp`) for immersive dynamic roll angles.

### 3. Hooke's Law Grappling Hook & 3D Aim Assist (`GrapplingGun.cs`, `GrappleCrosshair.cs`)
* Dynamic `SpringJoint` integration simulating Hooke's Law ($F = -k \Delta x - c v$) with customizable spring stiffness, damping, and distance ratios.
* Predictive 3D `SphereCast` aim-assist algorithm to ensure intuitive edge and platform latching.
* Procedural rope rendering in `LateUpdate` using Unity's `LineRenderer`.
* Dynamic crosshair UI with real-time state feedback (out of range, lock-on ready, actively grappling).

### 4. Finite State Machine (FSM) AI Hazards (`PumpkinWatcher.cs`, `PumpkinChaser.cs`)
* **Watcher Hazard:** Deterministic state machine (`SafeOrange` $\rightarrow$ `TransitionFlicker` $\rightarrow$ `DangerRed` $\rightarrow$ `JumpscareExecuting`) powered by a static, synchronized global clock (`_sharedTimer`).
* **Chaser Hazard:** Autonomous pursuit entity employing 3D vector navigation, smooth `Quaternion.Slerp` gaze tracking, and dynamic 3D spatial audio attenuation based on proximity.
* Line-of-Sight (LOS) target verification using raycasting with layer mask filtering to prevent false positives through solid obstacles.

### 5. Event-Driven Architecture & Bilingual Localization
* Decoupled respawn and checkpointing pipeline via C# `Action` delegates (`CheckpointManager.OnPlayerRespawned`).
* Full bilingual localization engine (English & Indonesian) for main menus, tutorial panels, settings, and ending sequence.
* Centralized master audio control with persistence via `PlayerPrefs`.

### 6. Custom Developer Tooling (`PumpkinLightingSetup.cs`)
* Unity Editor utility script integrated with `[MenuItem]`, `PrefabUtility`, and `Undo` APIs.
* Automates batch spotlight/pointlight hierarchy configuration and prefab instantiation across entire scenes, eliminating manual level setup bottlenecks.

---

## 🕹️ Controls

| Action | Primary Input (Keyboard & Mouse) |
| :--- | :--- |
| **Move** | `W` / `A` / `S` / `D` |
| **Look** | `Mouse Delta` |
| **Jump** | `Spacebar` |
| **Slide / Crouch** | `Left Ctrl` / `C` |
| **Grapple** | `Left Mouse Button` (Hold to swing, release to detach) |
| **Wall Run** | Run towards wall mid-air + `W` |
| **Wall Jump** | `Spacebar` while wall running |
| **Pause / Settings** | `P` / `Escape` |

---

## 📂 Project Structure

```text
Assets/
├── Scripts/
│   ├── PlayerMovement.cs          # Physics locomotion, slope handling, audio
│   ├── WallRun.cs                 # Raycast wall detection & normal propulsion
│   ├── GrapplingGun.cs            # SpringJoint Hooke's Law grappling physics
│   ├── GrappleCrosshair.cs        # Dynamic aim-assist HUD indicator
│   ├── PumpkinWatcher.cs          # Synchronized FSM line-of-sight hazard
│   ├── PumpkinChaser.cs           # Autonomous pursuit AI with 3D spatial audio
│   ├── PumpkinJumpscareManager.cs # Coroutine-based jumpscare & respawn sequence
│   ├── CheckpointManager.cs       # Event-driven checkpoint & state recovery
│   ├── MainMenuManager.cs         # Main menu, settings, bilingual localization
│   ├── PauseManager.cs            # Runtime pause menu and audio persistence
│   ├── FinishLine.cs              # Level completion & bilingual credits system
│   └── Editor/
│       └── PumpkinLightingSetup.cs # Custom Unity Editor tooling for batch setups
├── Scenes/
│   ├── main menu.unity            # Main menu scene
│   └── LV 1.unity                 # Level 1 gameplay scene
└── Prefabs/                       # Configured game assets and hazard prefabs
```

---

## 🚀 Getting Started

1. **Prerequisites:**
   * Install **Unity Hub** and **Unity 6** (`6000.6.5f1` or later).
2. **Clone the Repository:**
   ```bash
   git clone https://github.com/reinh20/WHAT-IS-UP-THERE.git
   ```
3. **Open Project:**
   * Launch Unity Hub $\rightarrow$ click **Add** $\rightarrow$ select the cloned project folder.
   * Open `Assets/Scenes/main menu.unity` or `Assets/Scenes/LV 1.unity`.
   * Press **Play** in the editor.

---

## 📜 License

Created by **Reinbrandt Malikiyano Cahyono** for the BZI Game Jam. All rights reserved.
