# Harbor City development notes

Status: 2026-09-23, after the 19:00 local-time gameplay smoke run. See `FEATURE_MATRIX.md` for the status of individual requested features and `SOL_FINAL_REPORT.md` for session timing and token accounting.

## Project and renderer

- Work is in `Unreal_GTA_GPT6Sol.uproject` and targets Unreal Engine **5.8.x**.
- The C++ runtime module is `Unreal_GTA_GPT6Sol`. The enabled Editor plugins are Python Script Plugin, Editor Scripting Utilities, and Modeling Tools Editor Mode. Enhanced Input is a runtime dependency.
- The Windows renderer targets DX12/SM6. Lumen global illumination and reflections, Virtual Shadow Maps, a movable directional sun, movable SkyLight, sky atmosphere, and height fog are used. Hardware ray tracing and Substrate are disabled.
- The visual direction is a stylized coastal city with graphite asphalt, sand and cream architecture, teal glass, and coral accents. Project-owned Blender meshes and generated Unreal materials supply the look.
- `Config/DefaultEngine.ini` sets `/Game/GTA/Maps/HarborCity` as both the Game default and Editor startup map, and `SolGameMode` as the default GameMode. There are no missions or objectives.

## Runtime architecture

| Component | Responsibility |
| --- | --- |
| `Core/SolGameMode` | Selects `SolPlayerCharacter` and `SolHUD`, ensures one `SolWorldDirector`, and runs the optional `-SolSmoke` test. |
| `Player/SolPlayerCharacter` | Enhanced Input actions created at runtime; third-person/first-person camera, movement, swim/breath, health/armor, skills, inventory, hitscan/melee combat, and vehicle entry. |
| `Vehicles/SolVehicle` | One vehicle actor with a swept box collision root; car variants, boat/air motion, traffic autopilot, lights/audio, damage, repair, and paint/engine values. |
| `World/SolNPC` | Civilian/police/tactical role, simple wandering/panic or pursuit, LOS, direct police damage, and death. Uses static character visuals. |
| `World/SolWorldDirector` | Local traffic/pedestrian population, wanted/search state, police spawning, time/weather, nearby services, 45-action admin menu, and SaveGame calls. |
| `World/SolSaveGame` | Player, inventory, skills, weather/time, and one stored garage vehicle. |
| `UI/SolHUD` | Canvas HUD, whole-city map, service prompts, wanted stars, vehicle readout, and admin overlay. This is not UMG. |
| `Weapons/SolWeaponTypes` | Fixed definitions for 11 weapon categories: damage, range, rate, spread, magazine, pellets, automatic/melee flags. |

The map is authored by Editor Python. Its source layout is `Automation/world_layout.json`; `Automation/build_world.py` consumes that file to build roads, districts, lighting, and points of interest. Current C++ traffic routes and HUD road coordinates are also hard-coded to the same grid. Changing the layout JSON alone will not update those runtime paths.

## World layout

The intended footprint is **600 × 600 m** (X and Y from -30,000 to +30,000 Unreal centimeters). Land covers a 600 × 490 m rectangle; the southern 600 × 110 m band is a water basin with a sea floor, visual water surface, pier, and promenade. The visual sea surface has collision disabled. The road grid is at X = -24,000, -12,000, 0, 12,000, 24,000 and Y = -16,000, -4,000, 8,000, 20,000 cm. Roads, sidewalks, crosswalks, traffic-light meshes, streetlights, buildings, and props are assembled in the map. A NavMeshBoundsVolume is present; NPC movement currently uses direct movement rather than navmesh paths.

| District | Approximate location | Content |
| --- | --- | --- |
| Downtown/commercial | Northeast and center-east | Towers, storefront massing, police station, civic streets. |
| Residential | Northwest | Houses/apartments, safehouse, clinic, clothing-shop marker. |
| Industrial/service | Southwest | Warehouses, containers, service depot, gas-station marker. |
| Waterfront | South edge | Promenade, marina, piers, sea basin, helipad. |
| Park | Northwest high edge | Trees, rocks, paths, viewpoint. |
| Aviation | Northeast high edge | Runway/airstrip, plane access, aircraft spawn space. |

There are **14 map POI markers**: player spawn, safehouse, clothing shop, weapon shop, police station, hospital, clinic, garage, service depot, gas station, marina, helipad, airstrip, and park viewpoint. Six locations have implemented proximity actions: weapon shop, garage, clinic, safehouse, pier, and airfield. The other markers give location context rather than working shop systems. The latest Editor validation counted 792 actors, including 732 StaticMeshActors.

## Controls

Controls are keyboard/mouse and are defined in `SolPlayerCharacter.cpp`; the director reads service/admin keys directly. Number keys select **inventory slots**, not fixed weapon types.

| Key | On foot | In a vehicle / menu |
| --- | --- | --- |
| `W A S D` | Move relative to camera | Throttle/reverse and steer. |
| Mouse | Orbit/look | Look with chase camera. |
| `Left Shift` | Sprint while stamina lasts | — |
| `Space` | Jump; rise while swimming | Handbrake; helicopter descent is also tied to handbrake. |
| `Left Ctrl` | Crouch; dive while swimming | — |
| `C` / `X` | Toggle stealth / enter or leave low cover | — |
| `Right Mouse` / `Left Mouse` | Aim / fire or melee | Aim / permitted one-handed fire. |
| `R` | Reload | Reload permitted firearm. |
| `Q`, mouse wheel, `1–9` | Previous/cycle/select owned inventory slot | Same selection. |
| `F` | Enter nearby vehicle | Exit vehicle. |
| `E` | Interact with a nearby service or looked-at actor/vehicle | Proximity services still read `E`; camera-trace interaction is only on foot. |
| `V` | Toggle third/first-person camera | Toggle camera. |
| `H` / `L` | — | Horn / headlights. |
| `Tab` or `F1` | Open/close admin menu | In menu: `Up/Down` select, `Enter` run, `Esc` or `F1` close. |
| `P` / `U` / `G` | At garage with vehicle: paint / engine upgrade / store or retrieve | Same garage actions when within range. |

The HUD shows key hints. `F1` is handled by the world director; `Tab` is an Enhanced Input action. The 45 admin entries include six teleports; compact car, boat, helicopter, plane, and seven extra vehicle variants; all 11 weapon categories; ammo; wanted level and police; health, money, vehicle repair; time/weather; traffic/pedestrian and invulnerability toggles; save/load; skills; FPS. Current menu can increment or clear wanted level; it has no direct arbitrary numeric entry.

## Gameplay systems

**Vehicles.** The drivable roster is compact car, sedan, police cruiser, SUV, pickup, van, sports car, motorcycle, motorboat, civilian helicopter, and small airplane. Vehicle classes are C++ enum/configuration variants using Blender-authored meshes. The main mesh is static; separate wheels and helicopter rotors exist in Blender/Unreal imports, but runtime does not animate them. Movement uses velocity and swept box movement with simple ground/sea/air rules, not Chaos Vehicle suspension or tire physics. Cars have throttle/reverse, steering, handbrake, health, collision damage, repair, lights, horn, an engine loop, and optional police siren. At zero health, radial damage and a fire light appear; there is no assigned explosion emitter. Entering an autopilot or police vehicle stops autopilot and registers a crime, without an actual NPC driver ejection animation.

**Traffic and NPCs.** The director generates local loop traffic around the player, aiming for up to 12 traffic cars. Forward sweeps slow/stop for obstacles. It also keeps up to 25 nearby civilians and removes distant ones. NPCs use short, direct movement targets, with simple panic/flee behavior; they do not follow NavMesh sidewalks or obey signal phases. Police/tactical actors pursue the current or last-known player location and apply direct damage if they have LOS and range.

**Wanted.** Crime severity accumulates pressure into 0–5 stars. Lower-severity events need witness/LOS; severity 2 or greater reports immediately. Police can maintain line of sight, lose it, search the last-known position, and let the wanted level decay while unseen. Level 4+ spawns tactical NPCs; higher levels may add police vehicles. There are no arrests, roadblocks, PIT, or police helicopters.

**Combat.** Fists, knife, bat, pistol, heavy pistol, SMG, shotgun, rifle, sniper, grenade, and launcher have definitions. Firearms use line traces; shotgun fires eight traces. Melee uses a short sphere sweep. Grenade and launcher currently cause radial damage at the trace impact, with no projectile travel. Visible held models are `SM_Pistol` or `SM_Rifle`; the imported `SM_Shotgun` is not selected by runtime. One-handed firearms are allowed during driving with increased spread. The player starts with fists and a pistol. Timed reload, reserve ammo, armor damage, death, and respawn are implemented.

**Player mobility and skills.** CharacterMovement provides walk, sprint, jump, crouch, and swim modes; coastal coordinate/depth checks track submerged breath and drowning. A low-cover trace enables crouched lateral movement, while stealth reduces sight range and footstep loudness. Skills implemented are Stamina, Shooting, Driving, Flying, and Lung. Strength and Stealth skill tracks from the target brief are absent.

**Services and save.** The arms shop buys the next unowned pistol/SMG/shotgun/rifle or ammo; clinic heals for $150; garage repairs for $250, cycles paint for $150, upgrades engine for $500 (three steps), and stores one vehicle; safehouse saves and advances time eight hours; pier spawns boat; airfield spawns helicopter. The save slot `HarborCitySandbox` contains player position, vitals, cash, inventory, skills, time/weather, and one stored vehicle configuration. It does not persist live traffic/NPCs or all world changes.

**Time, weather, UI, sound, VFX.** Time advances by one game hour per 180 real seconds. Clear, Cloudy, Rain, Fog, and Storm change fog density; Rain/Storm play the rain loop. The Canvas HUD displays vitals, cash/ammo, wanted stars and SEARCH/PURSUIT, clock/weather, fixed whole-city road map, service prompts, speed and vehicle health. Seven generated SoundWaves cover gunshot, engine, horn, siren, footsteps, rain, and city ambience. Muzzle, fire, and police lights are simple point-light effects. There are no Niagara effects, rain particles, bullet impacts, or complete audio mix.

## Blender to Unreal asset pipeline

The canonical scene is `UnrealGTAGPT6Sol.blend` in the project root. The dedicated `blender_unreal_gpt6sol` Blender instance is on **127.0.0.1:9881**; do not route these assets through other projects' Blender connections. `Tools/blender_mcp_client.py` opens a local MCP stdio session through `mcp-for-blender.exe --port 9881`. Run it with the Python interpreter installed beside that executable, so its MCP SDK is available. The project helper's `--object` export path checks the canonical `.blend` path before acting. Details and examples are in `SourceAssets/BlenderExports/README.md`.

The `Tools/blender_asset_*.py` recipes are sent to the connected Blender instance with MCP `execute_blender_code` alongside `Tools/blender_assets_core.py`. Source meshes live in separate `GTA_Assets` collections; vehicles/weapons face +X with Z up. The scene is saved, then individual FBX exports go to `SourceAssets/BlenderExports`. There are **34 FBX files**: 33 static mesh sources, including separate car wheel and helicopter rotor parts, plus one skinned `PlayerRig.fbx`. Unreal Editor Python `Automation/import_assets.py` creates `/Game/GTA/Generated/SM_*` and `SK_PlayerRig`, with static collision generation and material import. The level builder uses imported building/prop meshes and creates additional primitive geometry and 18 palette materials. The imported rig is preserved as a skeletal mesh but is not hooked into the runtime character or animations.

Audio is project-generated by `Automation/generate_audio.py` into `SourceAssets/Audio/*.wav` and imported under `/Game/GTA/Audio`. `Automation/update_audio_looping.py` sets loop flags for the appropriate SoundWaves.

## Build, launch, and repeatable checks

Double-click `LaunchHarborCity.cmd` on Windows or `LaunchHarborCity.command` on macOS for a direct free-roam launch. Alternatively, open the `.uproject` and press Play. Run the commands below from the project root in PowerShell; change `UE_ENGINE_ROOT` if Unreal Engine is installed elsewhere.

```powershell
$ueRoot = if ($env:UE_ENGINE_ROOT) { $env:UE_ENGINE_ROOT } else { Join-Path $env:ProgramFiles 'Epic Games\UE_5.8' }
$projectPath = (Resolve-Path .\Unreal_GTA_GPT6Sol.uproject).Path
$validationScript = (Resolve-Path .\Automation\validate_project.py).Path

# Build the C++ Editor target.
& "$ueRoot\Engine\Build\BatchFiles\Build.bat" Unreal_GTA_GPT6SolEditor Win64 Development "-Project=$projectPath" -WaitMutex

# Open the already-configured HarborCity map and press Play, or start directly in game mode.
& "$ueRoot\Engine\Binaries\Win64\UnrealEditor.exe" $projectPath
& "$ueRoot\Engine\Binaries\Win64\UnrealEditor.exe" $projectPath -game -log

# Validate imported packages, gameplay classes, and map construction without PIE.
& "$ueRoot\Engine\Binaries\Win64\UnrealEditor-Cmd.exe" $projectPath -run=pythonscript "-script=$validationScript" -unattended -nop4

# Exercise the project-owned runtime smoke path and inspect SOL_SMOKE PASS/FAIL in the log.
& "$ueRoot\Engine\Binaries\Win64\UnrealEditor.exe" $projectPath -game -SolSmoke -seconds=16 -unattended -nosplash -log
```

To regenerate content, use the Blender MCP pipeline first; run `Automation/import_assets.py`, then `Automation/generate_audio.py` (if regenerating sound), then `Automation/build_world.py`, and finally `Automation/validate_project.py` through `UnrealEditor-Cmd.exe -run=pythonscript`. Pass absolute paths with `-script=` because relative paths resolve against the engine executable directory. **`build_world.py` clears and recreates authored actors in HarborCity**; it is intended for a complete world rebuild and should not be run against manual level edits that need preserving. Project Python scripts are reproducible Editor workflows, not runtime dependencies.

## Validation and current limits

- `Saved/Logs/HarborValidation.json`, 2026-09-23 19:15 local: zero errors. The map loaded; 792 actors, 732 static mesh actors, 14 POI markers; 33 static meshes, `SK_PlayerRig`, seven SoundWaves, and six gameplay classes resolved. It also checked the 600 m footprint and non-colliding visual sea surface. This is an Editor commandlet check, not a PIE test.
- `Saved/Logs/SolSmokeFinal2.console.log`, 2026-09-23 19:00 local: `SOL_SMOKE PASS` with process exit code 0. It found a player, 7 civilians, 6 traffic cars, and exercised wanted setter, rifle grant, car enter/exit, paint/engine changes, and sedan spawning. It then measured car travel 3,249 cm, boat travel 6,213 cm, helicopter rise 1,605 cm, and plane travel 7,653 cm. There was no `SOL_VEHICLE_BLOCKED` warning in this run. The smoke checks selected paths and does not send mouse/keyboard input.
- The project is playable with the current C++ path, but the lack of animated runtime characters and moving wheel/rotor parts is visually obvious. Vehicle motion is intentionally simple. No full manual playtest or frame-rate benchmark is documented here.
- Police, civilians, shops, cover, swimming, all weapon hits, save/load round-trips, weather presentation, and the whole admin menu require further hands-on QA. Advanced traffic/police, UMG, Niagara, full audio, wildlife, taxi, parachute, activities, and other target features are incomplete or absent as detailed in `FEATURE_MATRIX.md`.
