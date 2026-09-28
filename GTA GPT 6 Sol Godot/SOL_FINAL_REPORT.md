# Harborline — GPT-6 Sol final report

## Result

The existing Godot project now opens directly into an original, missionless single-player free-roam sandbox. Harborline has an approximately **600 × 600 m** detailed coastal map, six regions, vehicles on land/water/in air, civilian traffic and pedestrians, combat, a 0–5 star wanted system, world services, HUD/maps, a benchmark console, day/night, weather, audio, VFX and basic save/load. The look is intentionally stylized rather than photorealistic.

The project uses **Godot 4.7.2 stable**, **Forward+ / D3D12**, **Jolt Physics 3D** and **GDScript**. All work is in the original project rooted at `project.godot`; no nested project was created. The main scene is `gta/scenes/bootstrap/main.tscn`. There are no quests, missions, scripted story steps or progression gates.

## What works in the available checks

The final headless smoke run ended with **0 failures**. It exercised map/population creation, moving civilian traffic, player walking and a pistol shot, compact-car entry/driving/exit, admin boat/airplane spawns, five-star police response, the lost-sight search transition, rain on/off, a weapon-shop purchase and suppressor, save/load of health and an occupied vehicle, and benchmark/weapon-wheel state. A separate 1,800-frame headless run and GUI visual check completed without errors. A targeted traffic check saw a vehicle stop at red and move 31.88 m after green; all 32 pedestrians loaded one of three Blender variants and idle/walk clips. This is focused verification, not proof of every control, vehicle class or weapon in long play.

## Map and free-roam systems

| Region | Main content |
| --- | --- |
| Downtown | Commercial blocks, civic plaza, armory, police station and hospital. |
| Residential | Houses, safehouse and smaller streets. |
| Industrial | Warehouses, service garage and gas station. |
| Waterfront | Beach edge, docks, marina, swimming water and boat access. |
| Green | Park/trees and quieter open space. |
| Airfield | Runway, helipad and starting aircraft. |

The road network has eight scripted traffic loops. Initial world population includes 18 moving civilian cars, additional parked/special vehicles and 32 pedestrians, with replenishment and distance-limited simulation. Some ground/building pieces are procedural geometry; prominent character, vehicle and modular environment assets were authored in Blender.

### Vehicles

| Class | Playable roster | Scope |
| --- | --- | --- |
| Cars | Compact, sedan, sport, SUV, pickup, van, police car | Common entry/exit and arcade driving, braking, reverse, handbrake, damage, horn and lights. Compact driving was smoke-tested. |
| Two-wheel | Motorcycle | Model and shared ground control with motorcycle profile/lean; not separately road-tested. |
| Water | Boat | Water-level movement controller; spawn path smoke-tested, boating not separately tested. |
| Air | Helicopter, airplane | Arcade lift/flight controllers; airplane spawn smoke-tested, full takeoff/landing not separately tested. |

Carjacking a traffic vehicle can create a theft crime and eject its visual AI driver. Vehicle health, impact damage, explosions and repair exist. The garage can change paint, engine and brakes. These are simplified systems; there is no detailed suspension, tire simulation, deforming damage, stored fleet or deep parts catalog.

### Weapons and character

The arsenal is **fists, knife, bat, pistol, heavy pistol, SMG, shotgun, rifle and sniper rifle**. Firearms use hitscan rays, ammo, reload, spread, recoil, tracers, impact effects and muzzle flash. The weapon shop sells ammo, armor and suppressor, extended-magazine, grip and optic mods. A weapon wheel slows time. Pistol firing and an SMG shop purchase were smoke-tested; the remaining weapons have code/data paths but were not individually regression-tested.

The player can walk, sprint, crouch, jump, aim, swim, enter/exit vehicles, use basic cover and stealth, fire selected weapons from a vehicle, deploy a simple parachute, take damage and respawn at the hospital. A first-person toggle exists. Cover/stealth, drive-by, diving and parachuting are limited versions of the requested systems; vaulting, ladders, dodge/block and ragdoll are absent.

### Wanted and police

Crimes add heat only when an officer sees them or a nearby civilian witness with line of sight survives long enough to report. Heat maps to **0–5 stars**. Police foot units and cars spawn; tougher tactical foot units can appear at high stars. If officers lose sight, pursuit changes to search around the last known location, then heat decays over time. The smoke test verifies 5-star spawning and the pursuit-to-search transition. Police do not yet use roadblocks, helicopters, cover/flanking or a busted/arrest sequence.

### Services and presentation

The armory sells weapons and mods. Hospital, safehouse, garage and gas station offer compact menus for healing, armor, saving/resting, outfit tint, repair and upgrades. The UI shows vitals, ammo, cash, wanted stars, local/full map, district, time/weather and vehicle speed. `F10` opens the benchmark console to teleport, spawn the implemented roster, set wanted/time/weather and access free-roam utilities. The clock, rain/fog presets, night lights, birds/fish visuals, ambient sounds and effects support recording.

## Blender assets

**33 GLBs** were exported from the project master `GodotGTAGPT6Sol.blend` through the dedicated `blender_godot_gpt6sol` connection on **port 9880**: 11 vehicles, five rigged characters, eight handheld weapons and nine environment/street/nature assets. The five characters each include a skeleton plus idle and walk clips. `gta/generated/models/ASSET_MANIFEST.md` records every file, scale and pivot. The live export client verifies the connected Blender file before executing export code, and Godot imported the exported GLBs. No asset pack or model from another project was used.

## Controls and launch

Open `project.godot` in Godot 4.7.2 and select **Run Project**; free roam starts immediately. Use `W A S D` to move/drive, mouse to orbit, `Shift` to sprint, `Space` to jump/handbrake (or lift/pitch while flying), `Ctrl`/`C` to crouch/descend, `F` for vehicle entry/exit, `E` for services, left/right mouse to fire/aim, `R` to reload, `Tab` for the weapon wheel, `Q` for cover, `X` for stealth, `V` for first person, `M` for map, `F10` for benchmark tools, `F5`/`F9` to save/load and `Esc` to pause. `H`, `L` and `J` operate horn, headlights and police siren. Full details are in `DEVELOPMENT.md`.

## Known limitations and performance

- Traffic follows fixed loops with basic signal phases and stop-on-red behavior. It lacks lane changes, comprehensive right-of-way and accident handling. Pedestrians use direct waypoint steering rather than navigation meshes or safe crossing logic.
- Ordinary NPCs now choose among three civilian looks and play imported idle/walk clips. Their pursuit/flee and combat reactions remain simple; there is no detailed shop staff, arrest, ragdoll or combat tactics.
- Aircraft, boating, cover, stealth, swimming, scuba, parachute and drive-by are playable foundations without the requested depth. No taxi passenger service, train, smartphone, interior, robbery loop or optional shooting/racing/sports activity exists.
- Save/load preserves player-centric state, time and weather. When saving inside a vehicle, it restores that vehicle's position, damage, paint and upgrades. Other spawned/destroyed vehicles, NPCs and world changes do not persist. Skills are tracked numerically but do not yet affect abilities.
- MultiMesh wildlife, population caps and distance checks control some cost. There is no measured sustained FPS benchmark on the target machine and no full LOD/streaming pipeline; the dense runtime-generated city may need tuning for lower-end hardware.
- The last available checks show no critical startup/parser failure. Headless ambience is skipped to avoid a Dummy AudioServer cleanup issue; audio plays in the GUI run. See `FEATURE_MATRIX.md` for per-feature status and the scope of verification.

## Build and verification

```powershell
godot --headless --editor --path . --quit
godot --headless --path . --script res://gta/tools/smoke_test.gd
godot --path . --script res://gta/tools/visual_qa.gd
```

Private development-session telemetry, local paths, and session identifiers are intentionally excluded from the public repository.
