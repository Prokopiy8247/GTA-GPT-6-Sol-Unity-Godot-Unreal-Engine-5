# Harbor City feature matrix

Updated 2026-09-23. This matrix describes the implementation, rather than the full target in `GPT-6-Sol_UnrealEngine5_GTA_BlenderMCP_Prompt.md`.

`WORKING` means an implemented path passed an Editor asset/map check or the 2026-09-23 19:00 local-time `-game -SolSmoke` run. `PARTIAL` means executable code or authored content exists, but the requested depth or direct gameplay validation is missing. `NOT IMPLEMENTED` means the requested system has no usable gameplay implementation. A smoke pass does not replace a complete interactive playtest.

## Project and world

| Feature | Status | Notes |
| --- | --- | --- |
| Open directly into free roam with no missions | WORKING | `HarborCity` is the Game and Editor default map; `SolGameMode` creates the player and world director. No mission system is present. |
| Approximately 600 × 600 m city | WORKING | Bounds are X/Y ±30,000 cm, with 792 map actors and 14 POI markers in the latest Editor validation. The southern strip is water. |
| Varied urban districts | WORKING | Downtown, residential, industrial, waterfront, park, and aviation areas; roads, sidewalks, crossings, pier, runway, lighting, and props. |
| Accessible interiors | PARTIAL | Simple service structures and locations exist; there is no detailed enterable interior system. |
| World streaming and large-world tooling | NOT IMPLEMENTED | Compact single map; no World Partition streaming, HLOD, PCG, or Data Layers. A NavMesh bounds actor exists, but NPC movement does not use navigation paths. |
| Original Blender-authored visible assets | WORKING | Dedicated Blender scene produced 33 static FBX files plus `PlayerRig.fbx`; all 34 mesh imports resolve in Editor validation. Runtime uses authored player, NPC, vehicle, weapon, building, and prop meshes. |
| Character rig and animation | PARTIAL | `SK_PlayerRig` imports, but the running character uses `SM_Player`; no Animation Blueprint, locomotion animation, IK, or ragdoll. |
| Material and lighting direction | PARTIAL | Stylized material palette, Lumen, Virtual Shadow Maps, movable sun, SkyLight, atmosphere, and fog are present. No final visual/performance review, Nanite/HLOD pass, or broad material variation. |

## Player and combat

| Feature | Status | Notes |
| --- | --- | --- |
| Third-person player and camera | PARTIAL | Player spawn passed smoke; C++ movement, spring arm, orbit, collision test, aim zoom, and vehicle camera exist, but input/camera were not exercised by automated smoke. |
| First-person toggle | PARTIAL | `V` moves the spring arm to zero and hides the body; first-person weapon presentation and camera polish are limited. |
| Walk, sprint, jump, crouch | PARTIAL | Enhanced Input and CharacterMovement code are present; no direct automated key-input check. |
| Swimming, diving, breath, drowning | PARTIAL | Coastal water zone switches to swim mode; `Space` rises, `Left Ctrl` dives, submerged breath depletes, drowning applies damage. No extended water playtest or underwater art/audio. |
| Health, armor, death, respawn | PARTIAL | Damage absorbs armor, death clears wanted level, and a timer respawns at clinic with replenished vitals. No death screen, ragdoll, or fall-impact presentation. |
| Weapon inventory and switching | WORKING | Smoke grants a rifle and confirms inventory entry; code supplies 11 weapon types, slot/cycle selection, magazines, reserve ammo, and timed reload. Individual weapons were not all fired in smoke. |
| Firearms and melee damage | PARTIAL | Hitscan firearms, 8-pellet shotgun, short sphere-sweep melee, spread, rate limits, and damage calls are implemented. No automated hit/damage or balance playtest. |
| Grenade and launcher | PARTIAL | Both use immediate trace impact plus radial damage; there are no thrown or flying projectiles, trajectories, or explosion particles. |
| Weapon customization and ballistics | NOT IMPLEMENTED | No attachments, suppressors, weapon upgrades, projectile drop/penetration, or material-specific impacts. |
| Weapon wheel | NOT IMPLEMENTED | Inventory switching uses keys and mouse wheel; no radial selector or slowed-time weapon menu. |
| Drive-by shooting | PARTIAL | One-handed firearms are permitted from vehicles with larger spread. No dedicated seating/aim animation, passenger shooting, or AI drive-by combat. |
| Cover | PARTIAL | `X` detects a low wall, crouches, allows lateral movement, and adds blind-fire spread. No cover poses, peeking, corner transitions, or robust cover navigation. |
| Stealth | PARTIAL | `C` slows/crouches the player and reduces NPC sight distance and footstep volume. No suspicion state, stealth takedown, or suppressor mechanics. |
| Traversal and advanced melee | NOT IMPLEMENTED | No vault/mantle, ladders, dodge, block, knockdown, directional hit reactions, or ragdoll. |
| Character skills | PARTIAL | Stamina, Shooting, Driving, Flying, and Lung progress and affect selected calculations; no Strength or Stealth skill, skill UI, or balancing pass. |

## Vehicles, traffic, and services

| Feature | Status | Notes |
| --- | --- | --- |
| Vehicle enter/exit and car movement | WORKING | Smoke checks enter/exit and an autopilot car moving 3,249 cm. Player steering, reverse, handbrake, collision, and chase camera are coded. |
| Road vehicle roster | PARTIAL | Compact, sedan, police cruiser, SUV, pickup, van, sports car, and motorcycle variants are selectable with imported meshes. Smoke checks a sedan spawn but only moves one car; no bicycle, muscle car, taxi, truck, ambulance, or fire truck. |
| Boat, helicopter, and airplane motion | WORKING | Latest smoke measured boat travel 6,213 cm, helicopter rise 1,605 cm, airplane travel 7,653 cm. Movement is simplified; handling and landing were not comprehensively tested. |
| Vehicle physics and handling | PARTIAL | Swept box movement, per-class speed, simple ground clearance/buoyancy/lift and collision damage. No Chaos wheels/suspension, tire-grip model, true fluid simulation, or rigidbody dynamics. |
| Vehicle damage, fire, explosion | PARTIAL | Health, repair, collision damage, radial explosion damage, and warning/fire lights exist. `ExplosionEffect` has no assigned emitter; mesh deformation and debris are absent. |
| Vehicle lights, horn, siren, audio | PARTIAL | Headlights, brake light, horn, speed-pitched engine loop, and police siren hooks exist. No rotating/animated wheels or rotors, doors, separate reverse lights, or detailed engine audio. |
| Carjacking | PARTIAL | Entering an autopilot or police vehicle stops it and records a crime. There is no visible NPC driver to eject or driver reaction. |
| Traffic simulation | WORKING | Smoke found 6 traffic actors. Road-loop autopilot spawns/despawns around player, caps at 12, steers and brakes for forward sweep obstacles. |
| Intersections and advanced traffic | NOT IMPLEMENTED | Decorative traffic-light meshes do not run signal phases; no lane changes, accident response, visible drivers, or meaningful right-of-way. |
| Garage repair, paint, engine upgrade | WORKING | Smoke confirms paint preset and engine level change. Nearby garage offers paid repair, 5 paint presets, and 3 engine upgrades. |
| Garage storage and deeper customization | PARTIAL | One vehicle's type, paint, and engine upgrade can be stored/retrieved and saved. No cosmetic parts, wheel/rim/tint work, tuning UI, or multiple owned vehicles. |
| Weapon shop, clinic, safehouse, dock, airfield | PARTIAL | Proximity `E` actions buy weapons/ammo, heal, save/rest, spawn a boat, or spawn a helicopter. No shopkeeper dialogue, menus, or transaction playtest. |
| Economy | PARTIAL | Starts at $1,500; service prices and admin cash action work in code. No jobs, property, sustained income, or economy balance. |
| Clothing, hair, phone, and personal services | NOT IMPLEMENTED | Clothing-shop POI is decorative. There are no appearance choices, smartphone, contacts, or related menus. |

## NPCs, police, and world response

| Feature | Status | Notes |
| --- | --- | --- |
| Pedestrian population | WORKING | Latest smoke counted 7 civilians. Director locally spawns up to 25 and removes distant civilians; they wander on simple targets. |
| Civilian panic and witness checks | PARTIAL | Nearby civilians panic and flee; low-severity crime needs a nearby NPC/police sight check, while severe crime reports immediately. No delayed phone call, suspicion, crowd behavior, or pedestrian road rules. |
| Police response and 0–5 stars | PARTIAL | Smoke set and read 3 stars. Pressure maps to 0–5, officers/tactical units spawn, pursue, and damage the player. Other levels and a sustained chase were not exercised in smoke. |
| Search and wanted decay | PARTIAL | LOS updates last known position; losing sight changes HUD to SEARCH and decays stars over time. No arrest/BUSTED, roadblocks, PIT, police helicopter, or coordinated search. |
| NPC combat and animations | PARTIAL | Police/tactical direct damage is gated by LOS/range; civilians can panic and die. No weapon firing visuals, cover tactics, ragdoll, visible drivers, or animation variants. |
| Physics and hit reactions | PARTIAL | Vehicle collisions apply damage and dead NPC meshes tip over; no physical ragdolls, destructible props, knockback, or material-aware reactions. |
| Wildlife and emergency services | NOT IMPLEMENTED | No animals, ambulances, fire trucks, or responder simulation. |

## Presentation, persistence, and sandbox

| Feature | Status | Notes |
| --- | --- | --- |
| HUD and map | PARTIAL | C++ Canvas shows health, armor, cash, ammo, stars, weather/time, service prompts, speed, vehicle health, and a fixed whole-world road map. No heading, POI markers, GPS, dynamic zoom, or UMG. |
| Admin benchmark menu | PARTIAL | 45 C++ Canvas actions cover teleports, vehicle/weapon spawning, wanted level, weather/time, health, cash, population, save/load, skills, and FPS. Smoke only exercised selected actions indirectly. |
| Day/night and weather | PARTIAL | Time advances; movable sun rotates; Clear, Cloudy, Rain, Fog, Storm adjust fog and rain audio. No rain geometry, wet roads, weather-driven handling, or weather AI. |
| Audio | PARTIAL | Seven project-generated SoundWaves import and resolve: gunshot, engine, horn, siren, footsteps, rain, and city ambience. Missing reloads, impacts, explosions, ocean, rotor, radio, and UI sound. |
| VFX | PARTIAL | Muzzle, vehicle fire, and siren use dynamic light. No configured Niagara emitters, bullet impacts/decals, rain particles, splashes, tire smoke, or visible explosion burst. |
| Save/load | PARTIAL | SaveGame code persists player transform/vitals/cash/inventory/skills, time, weather, and one garage vehicle. No smoke round-trip, world NPC/traffic persistence, or menu feedback. |
| Taxi, radio, parachute, scuba gear, hobbies | NOT IMPLEMENTED | No usable gameplay systems for these requested optional activities. Basic diving exists without scuba gear. |
| Map navigation and public transit | NOT IMPLEMENTED | No GPS route, waypoint planner, transit vehicles, stations, or passenger service. The HUD's fixed road map is listed separately. |
| Performance/scalability pass | PARTIAL | Local population caps, spawn/despawn, and coarser director/NPC ticks exist. No measured FPS target, pooling, HLOD/instancing study, or long-session profiling. |

## Validation evidence

- `Saved/Logs/HarborValidation.json` from 2026-09-23 19:15 local time: zero errors; map loads; 792 actors, 732 static mesh actors, 14 POIs; 33 static meshes, one skeletal mesh, seven SoundWaves, and six gameplay classes resolve.
- `Saved/Logs/SolSmokeFinal2.console.log` from 2026-09-23 19:00 local time: `SOL_SMOKE PASS`; player spawned; 7 civilians; 6 traffic vehicles; wanted-level setter, rifle inventory, enter/exit, garage paint/upgrade, sedan spawn, car travel (3,249 cm), boat travel (6,213 cm), helicopter rise (1,605 cm), and plane travel (7,653 cm) passed.
- These checks do not certify every interaction, weapon hit, save/load round trip, art presentation, animation, frame rate, or long-running AI behavior.
