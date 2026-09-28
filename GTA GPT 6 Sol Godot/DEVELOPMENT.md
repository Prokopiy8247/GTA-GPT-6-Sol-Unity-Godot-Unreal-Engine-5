# Harborline — development guide

Harborline is an original, missionless, single-player coastal sandbox. The configured main scene opens directly into free roam. Its deliberately stylized look uses warm concrete, dark roads, teal water and glass, and amber night lighting. No downloaded 3D asset pack is required.

## Engine and project layout

- **Engine:** Godot `4.7.2.stable.official.ed1daf0bf`. Install Godot 4.7.2 and use its editor or console executable from your own machine.
- **Renderer:** Forward+ with the Windows D3D12 rendering device setting. Procedural sky, filmic tonemapping, shadow-casting sun, selected-weather fog and night lights.
- **Physics:** Jolt Physics 3D. Player, NPCs and vehicles use `CharacterBody3D` with simple collision shapes and project-owned movement code.
- **Language:** GDScript, without C#.
- **Entry scene:** `gta/scenes/bootstrap/main.tscn`.

| Location | Purpose |
| --- | --- |
| `gta/code/core/game.gd` | Startup, population/police director, interactions, economy, clock/weather, save/load and benchmark actions. |
| `gta/code/world/world.gd` | Deterministic map, six districts, roads, landmarks, lighting, props and route data. |
| `gta/code/world/ambient_life.gd` | Distance-limited bird/fish visuals using MultiMesh. |
| `gta/code/player/` | Character scene, camera, combat, inventory, swimming, cover and vehicle entry. |
| `gta/code/vehicles/` | Shared vehicle scene/controller for land, water and air. |
| `gta/code/ai/npc.gd` | Civilian wandering/fleeing and police pursuit/search. |
| `gta/code/police/wanted.gd` | Crime heat, 0–5 stars, pursuit/search and timed decay. |
| `gta/code/ui/` | HUD, maps, weapon wheel, shop, pause and benchmark menus. |
| `gta/generated/models/` | 33 Blender-exported GLBs and an asset manifest. |
| `gta/generated/audio/` | Eight project-generated WAV cues. |
| `gta/tools/` | Blender MCP export, audio generator and QA scripts. |

The runtime assembles the detailed roughly **600 × 600 m** ground map from authored GLB modules and generated geometry. Its districts are downtown, residential, industrial, waterfront, green/park and airfield. Simplified ocean extends south of the detailed ground area. The route graph has eight traffic loops and about 200 pedestrian points. Startup places 18 moving civilian cars, parked vehicles, a boat, a helicopter, an airplane and 32 pedestrians. Population and AI activity are capped or distance-limited. There is no mission or quest system.

## Controls

| Input | Action |
| --- | --- |
| `W A S D`, mouse | Move/steer; orbit camera. |
| `Shift`, `Ctrl` or `C` | Sprint; crouch or dive in water. In a helicopter, Ctrl/C descends. |
| `Space` | Jump and deploy parachute when falling; ground-vehicle handbrake; helicopter rise; airplane pitch up. |
| Left mouse, right mouse, `R` | Fire/melee, aim, reload. Sidearms/SMG can fire from a vehicle while aiming. |
| `1`–`9`, mouse wheel, hold `Tab` | Select owned weapons; Tab opens the slow-time weapon wheel. |
| `Q`, `X`, `V` | Enter/leave cover, toggle stealth, toggle on-foot first person. |
| `F`, `E` | Enter/exit a nearby vehicle; interact with a nearby service. |
| `H`, `L`, `J` | Vehicle horn, headlights, police siren. |
| `M`, `F10`, `Esc` | Full map, benchmark console, pause/close. |
| `F5`, `F9` | Save and load the free-roam state. |

The benchmark console teleports between districts, spawns the implemented vehicle/weapon roster, sets wanted level, time and weather, and provides repair, health, skills and population toggles. Its `taxi` option spawns an ordinary sedan for testing; it does not provide taxi passenger service.

## Systems

- **Player:** Third-person spring-arm camera, walking/sprint/crouch/jump, optional first person, basic swimming and breath, simplified parachute, shoulder aiming, fall damage, armor/health, death and hospital respawn.
- **Combat:** Fists, knife, bat and six firearms with damage/range/cadence, ammo and reload. Raycast hits, tracers, impact sparks, muzzle flash and camera recoil. Weapon shop mods: suppressor, extended magazine, grip and optic.
- **Vehicles:** Compact, sedan, sport, SUV, pickup, van, police car, motorcycle, boat, helicopter and airplane. Entry/exit, arcade-like class-specific control, horn/lights/siren, engine tone, impact damage and explosions. Garage repairs, six paint colors, three engine and three brake upgrade levels.
- **Population and wanted:** Cars follow fixed lane loops. Pedestrians choose among three civilian Blender looks, play imported idle/walk clips, wander waypoint points and flee gunfire; distant pedestrian simulation pauses. Witnesses use range/raycast sight and a short report delay. Police can witness directly; heat raises 0–5 stars, spawns officers/cars, and changes from pursuit to search on lost sight. Search follows the last known position and reduces heat over time. Tactical officers appear at high stars.
- **Services:** Weapon shop, hospital, safehouse, garage and gas station menus. Cash buys services and can come from civilian kills or benchmark tools. Safehouse saves, advances time and cycles outfit tints.
- **Presentation:** HUD for health, armor, ammo, cash, stars, district, time/weather and vehicle speed; minimap/full map; clear/cloudy/rain/fog/storm presets; rain, night lights, generated audio, and visual birds/fish.
- **Persistence:** `user://harborline_save.json` stores player position, health, armor, cash, inventory/mods, outfit, time, weather and numeric skills. Saving inside a vehicle also restores that vehicle's position, damage, paint and engine/brake upgrades. Other vehicles, destroyed objects, police/NPC state and world edits are not serialized. Skills currently increase as values but do not alter handling.

## Blender MCP workflow

`GodotGTAGPT6Sol.blend` is the master source. The project-specific Blender MCP connection uses **port 9880**. `gta/tools/run_export_blender_assets_mcp.py` verifies the connected `.blend` path, executes `gta/tools/export_blender_assets.py` through live MCP, saves the source, and exports GLBs to `gta/generated/models/`. Use only this project connection.

```powershell
uv run --no-project --with mcp-for-blender python gta/tools/run_export_blender_assets_mcp.py
uv run --no-project --with mcp-for-blender python gta/tools/run_export_blender_assets_mcp.py --only car_suv,player
```

The 33 GLBs comprise 11 vehicles, five characters, eight handheld weapons and nine building/street/nature assets. Character GLBs contain skeletons and idle/walk clips. The full inventory and dimensions are in `gta/generated/models/ASSET_MANIFEST.md`. The player and civilian/police NPCs play imported idle/walk clips; combat and physical reactions remain simple.

## Run and verify

Open this existing `project.godot` in Godot 4.7.2 and select **Run Project**. The main scene starts directly in free roam. From the project root:

```powershell
godot --headless --editor --path . --quit
godot --headless --path . --script res://gta/tools/smoke_test.gd
godot --path . --script res://gta/tools/visual_qa.gd
```

The smoke test covers scene creation, district/route/population counts, walking, moving traffic, one firearm, car entry/driving/exit, admin spawns, wanted/search, weather, a shop purchase, player and occupied-vehicle save/load, and UI state. It is a focused regression check, not exhaustive validation of every vehicle or weapon. `visual_qa.gd` writes captures to `.sol-run/`; pass `-- night`, `-- map` or `-- wheel` after the script argument for alternate views. GUI sound plays normally; headless mode skips ambience to avoid a Dummy AudioServer cleanup issue.

## Known limitations

- Vehicles are kinematic with box collisions; there is no articulated suspension, tire simulation or body deformation. Aircraft, boat, swimming, parachute and scuba handling are simplified.
- Traffic uses scripted loops and basic signal stopping. Intersections lack full right-of-way, lane changing and accident management. NPCs use direct steering rather than NavigationMesh, with no traffic-aware crossing.
- Cover permits attachment and side movement, but corner transitions, peeking/blind-fire poses and police cover tactics are incomplete. Stealth changes speed, noise and witness/sight thresholds without a layered suspicion model. Melee lacks block/dodge and directional reactions.
- Many world surfaces and secondary details are generated geometry. No accessible building interiors, full ragdoll, detailed character reactions, taxi passenger service, trains or interactive wildlife are present.
- Services and saved state support the basic sandbox loop. There is no smartphone, deep clothing/hair customization, stored garage collection, broad economy, gameplay skill effects or optional sports/race activities.
