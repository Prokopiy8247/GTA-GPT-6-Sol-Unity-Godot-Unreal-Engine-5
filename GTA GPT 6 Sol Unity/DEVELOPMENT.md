# Harborline development guide

## Project and design

Harborline is an original, missionless single-player free-roam sandbox built **inside the existing Unity project**. The project uses Unity **6000.6.0f1**, Universal Render Pipeline **17.6.0**, Input System **1.20.0**, and the PhysX physics backend. The visual direction is a compact, colorful coastal city: warm concrete, deep blue, teal water, coral accents, and geometric silhouettes. There are no quests, story missions, or mission markers.

The playable ground is approximately **600 × 600 m** (`x` and `z` about `-300..300`). The generated scene is [Harborline.unity](Assets/GTA/Scenes/Harborline.unity), and it is the enabled first build scene. The main districts are downtown/civic center, Westhaven residential, Foundry Quarter industrial, North Quay waterfront, Juniper Park, and Cape Airfield. The road grid connects six traffic loops, a port road, and a short runway. Nine interaction points cover the weapon shop, garage, safehouse, hospital, clothing shop marker, gas station, airfield, dock, and police station. The clothing marker, airfield marker, dock marker, and police marker have no full service menu yet.

## Code and scene architecture

All gameplay code is in [Assets/GTA/Code/Runtime](Assets/GTA/Code/Runtime). The main pieces are:

| Component | Responsibility |
| --- | --- |
| `SandboxPlayer`, `SandboxLimbAnimator` | CharacterController locomotion, health, armor, swimming, parachute, cover/stealth flags, procedural limb motion, vehicle entry. |
| `SandboxCamera` | Third-person orbit/chase camera, shoulder aim, obstacle avoidance, first-person toggle. |
| `SandboxVehicle`, `SandboxVehicleFeedback` | Rigidbody land/water/air physics, damage, lights, wheel/rotor animation and audio cues. |
| `SandboxTrafficAgent`, `SandboxPedestrian` | Waypoint traffic, civilian roaming/panic/witness behavior, police/tactical foot response. |
| `SandboxArsenal`, `SandboxFX` | Inventory, hitscan/projectiles, reload, impacts, explosions and generated tones. |
| `SandboxDirector` | World clock, weather, population caps, wanted/search state, services and JSON save/load. |
| `SandboxHUD`, `SandboxLocation` | IMGUI HUD, map, weapon selector, admin menu and location menus. |
| `SandboxSmokeCapture` | Optional standalone startup check enabled by `-smokecapture <png path>`. |

The deterministic editor builder [HarborlineBuilder.cs](Assets/GTA/Code/Editor/HarborlineBuilder.cs) creates the scene, materials, roads, buildings, location signs, Blender model instances, collisions, player, camera, 25 initial NPCs, 24 initial vehicles and traffic routes. Its fixed seed is `68314`. [HarborlineAudit.cs](Assets/GTA/Code/Editor/HarborlineAudit.cs) checks key scene objects, each of the five vehicle classes, material references and build order. [HarborlineBuildPlayer.cs](Assets/GTA/Code/Editor/HarborlineBuildPlayer.cs) produces the Windows player. Rebuilding the scene replaces it, so edit the builder for persistent scene changes.

The save file is `harborline-save.json` under Unity's `Application.persistentDataPath`. It records player position, vitals, cash, hour, weather, weapon ownership/ammunition and seven skill counters. Dynamic world state, owned vehicles, appearance and wanted pursuit do not persist. The game loads a present save on startup and also saves periodically.

## Default keyboard and mouse controls

| Control | Action |
| --- | --- |
| `WASD` | Move on foot; throttle/reverse and steer in vehicles; plane `W/S` changes throttle and `A/D` rolls. |
| Mouse movement / right button / left button | Orbit camera / aim / attack or shoot. |
| `Left Shift` | Sprint on foot. |
| `Space` | Jump, surface while swimming, slow parachute descent, land vehicle handbrake or helicopter climb. |
| `Left Ctrl` | Crouch on foot, dive while swimming or lower helicopter. `C` also crouches on foot. |
| `F` | Enter or exit nearby vehicle. |
| `E` | Interact with a nearby service point; plane yaw right. |
| `Q` | Enter or leave cover on foot; plane yaw left. |
| `Z` / `P` | Toggle stealth / deploy parachute during free fall. |
| `V` | Toggle first-person camera. |
| `R` | Reload. |
| `1`–`8`, mouse wheel, `Tab` | Select a weapon slot, cycle owned weapons, or hold the weapon selector. |
| `M` / `F1` / `F2` / `Esc` | Map / sandbox admin menu / controls / pause or close a menu. |
| `H` / `L` / `J` | Horn / headlights / police siren (where available). |
| Up/Down arrows or `I/K` | Plane pitch. |

The `F1` sandbox menu provides teleports to all six regions; spawn entries for the vehicle catalog; weapons and ammunition; wanted level `0..5`, police response, cash, health and armor; time and weather; traffic and pedestrian toggles; parachute, breath and scuba tools; save/load and skill counters. The wildlife toggle currently has no animal system behind it. Controller support is not implemented.

## Running and rebuilding

1. Open **this project folder** in Unity Hub with Editor **6000.6.0f1**. Open `Assets/GTA/Scenes/Harborline.unity` if Unity does not restore it, then press Play. Free roam starts immediately.
2. Ready-to-play Windows and macOS archives are published on the repository's GitHub Releases page.
3. In Unity, use **GTA → Build Harborline** to regenerate the scene, **GTA → Audit Harborline Scene** to verify it, and **GTA → Build Windows Player**, **Build macOS Player**, or **Build Windows + macOS Players** to create standalone builds. Wait for asset import and C# compilation to finish before running these actions.

For unattended execution on this machine, close other Unity processes using the same project first, then run from PowerShell at the project root:

```powershell
$UnityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe'
& $UnityEditor -batchmode -nographics -quit -projectPath (Get-Location).Path -executeMethod HarborlineAudit.Run -logFile 'Logs/harborline-audit.log'
& $UnityEditor -batchmode -nographics -quit -buildTarget win64 -projectPath (Get-Location).Path -executeMethod HarborlineBuildPlayer.BuildWindows -logFile 'Logs/harborline-windows-build.log'
& $UnityEditor -batchmode -nographics -quit -projectPath (Get-Location).Path -activeBuildProfile 'Assets/Settings/Build Profiles/macOS Universal.asset' -build 'Builds/Harborline-macOS/Harborline.app' -logFile 'Logs/harborline-macos-build.log'
```

The Windows player can perform an opt-in scripted check with `-smokecapture <absolute PNG path> -logFile <log path>`. It captures the camera into a RenderTexture, creates virtual Input System keyboard/mouse devices when needed in batch mode, injects movement, `F1` and one pistol shot, enters/drives/exits a car, sets wanted level 3 and rain, then exits. `-smokesave` enables a JSON save/load round trip at the isolated `.sol-run/harborline-smoke-save.json` path; ordinary player saves are skipped during smoke runs. The test does **not** establish manual control feel, menu clicks, sustained vehicle handling or recording performance. The clean batch smoke logs `hud-ui-image=UNAVAILABLE_BATCH` and `admin-ui-image=UNAVAILABLE_BATCH`; its world RenderTexture captures are visible, but HUD/admin appearance needs a visible-window review.

## Blender source and exports

The master source is [UnityGTAGPT6Sol.blend](UnityGTAGPT6Sol.blend). [BLENDER_ASSETS.md](BLENDER_ASSETS.md) lists the **38** authored FBX exports in [Assets/GTA/Generated/Models](Assets/GTA/Generated/Models), their use and coordinate conventions. The scene builder imports these FBXs, maps their material slots to URP materials, fits their scale, and creates simple Unity colliders and gameplay components. Local automation and session files are intentionally excluded from the public repository.

The separate rigged player FBX imports as a Generic rig with two clips, but the current game visual uses the segmented player with procedural limb motion. Its rig and animations are retained as a candidate for a later Animator integration.

## Current limitations and validation

The final editor scene audit recorded **24 vehicles, 25 actors, 9 services, 6,851 renderers, 4,995 imported-model renderer instances, zero missing materials, zero curb blockers, zero actors inside architecture, zero spawn blockers, a valid spawn ground check and zero audit issues**. A Windows player build succeeded and the clean batch-mode standalone smoke exited with code 0 and no logged exceptions. It found **25 runtime vehicles, 31 actors and 9 services**; moved the player **2.78 m** with virtual Input System `S`; opened `F1`; fired a pistol round (**15→14**); restored cash and ammo in an isolated JSON save/load round trip; activated pursuit at wanted level 3; entered a car; and selected rain. Civilian 18 moved **1.28 m**. Pickup 04 and Port Van 08 each moved **48.02 m**; SportsCar 02 moved **24.42 m** but was stopped at the five-second observation, without damage. The driven sports car covered **65.09 m in four seconds**, crossed `z=-80`, maintained a **180°** heading, reached **32.86 m/s** at the end of held `W` and retained **180/180** health. RenderTexture views show the boulevard, beach, park, airfield and imported models. The clean batch run reports HUD/admin UI images as unavailable; two earlier hidden non-batch starts intermittently stalled during graphics/input initialization, so visible-window startup and UI need further checks. See `.sol-run/scene_audit.txt`, `.sol-run/standalone_clean.log`, [FEATURE_MATRIX.md](FEATURE_MATRIX.md) and [SOL_FINAL_REPORT.md](SOL_FINAL_REPORT.md) for evidence.

Several systems are intentionally basic or absent: no full cover peeking/blind fire, contextual traversal, ragdolls, arrest state, roadblocks/PIT, transit, wildlife, radio, clothing workflow, persistent vehicle ownership, activities or interiors. Traffic uses waypoint loops and proximity braking rather than signal phases or lane changing. There is no active LOD or occlusion authoring pass, measured FPS benchmark, or manual keyboard/mouse play pass. The scripted car crossed one road line cleanly; reliable handling across longer routes remains under QA. The runway is short; aircraft handling still needs a takeoff/landing play test. Do not infer full mechanic quality from presence of a model or component.
