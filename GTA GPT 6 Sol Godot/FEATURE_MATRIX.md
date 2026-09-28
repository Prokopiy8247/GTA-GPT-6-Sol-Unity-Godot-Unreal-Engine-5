# Feature matrix

`WORKING` means a functional in-game path exists and the surrounding system survived smoke/visual checks; these checks are not exhaustive for every control. `PARTIAL` means code or content exists but the requested depth or per-class verification is incomplete. `NOT IMPLEMENTED` means no functional path. The [development guide](DEVELOPMENT.md) gives controls and architecture; `gta/tools/smoke_test.gd` defines the automated regression scope.

| Feature | Status | Notes |
| --- | --- | --- |
| Immediate free roam; no missions | WORKING | Main scene starts with direct player control. No quest/campaign code or mission markers. |
| Approximately 600 × 600 m detailed map | WORKING | Runtime-generated coastal footprint and simplified sea beyond it. |
| Six distinct map regions | WORKING | Downtown, residential, industrial, waterfront, green/park and airfield; six-region check passes. |
| Roads, walkways, docks, runway and landmarks | WORKING | Road grid and eight lane loops; police, hospital, armory, garage, gas station, safehouse and aviation sites. |
| Authored environment modules and props | PARTIAL | Blender office, house, warehouse, streetlamp, signal, tree, bench, bin and hydrant; many surfaces/details use generated geometry. |
| Blender MCP source/export pipeline | WORKING | Dedicated port 9880 checks the master `.blend`, saves it and exports 33 GLBs. |
| Rigged characters and animation | PARTIAL | Five character GLBs contain skeletons and idle/walk clips; player and NPCs switch clips, without broader action/reaction animation. |
| Third-person player and camera collision | WORKING | `CharacterBody3D`, SpringArm3D and camera are active; walking is smoke-tested. |
| Walk, sprint, jump and crouch | WORKING | On-foot controls and transitions implemented; walking exercised by smoke test. |
| On-foot first-person toggle | PARTIAL | Implemented in player controller; no focused camera/weapon clipping QA. |
| Contextual cover | PARTIAL | Nearby-wall attachment, low-cover detection, side movement and firing; no robust corner/peek or AI cover. |
| Stealth and takedown | PARTIAL | Slower/quieter stance, smaller witness radius and rear-melee damage bonus; no suspicion model. |
| Vault, mantle, ladder, roll and ragdoll | NOT IMPLEMENTED | No movement/physics path for these. |
| Swimming, underwater breath and damage | PARTIAL | Surface/dive movement and drowning timer; no rich diving environment. |
| Scuba and underwater exploration | PARTIAL | Scuba toggle prevents breath loss; underwater content/equipment absent. |
| Parachute | PARTIAL | Falling deploy/slowed descent and simple canopy; no full canopy steering or animation. |
| Melee | PARTIAL | Fists, knife and bat raycast attacks; no block, dodge or directional reactions. |
| Firearms and ammo/reload | PARTIAL | Pistol, heavy pistol, SMG, shotgun, rifle and sniper use distinct profiles; pistol firing is smoke-tested, not every weapon. |
| Weapon wheel and inventory | WORKING | Owned-weapon wheel slows time and restores it; smoke-tested. |
| Weapon modifications | PARTIAL | Suppressor, extended magazine, grip and optic have effects; only suppressor purchase is smoke-tested. |
| Projectile interaction and combat VFX | PARTIAL | Hitscan, spread, impact sparks, tracers, muzzle flash; no physical projectile or material penetration system. |
| Car enter, drive and exit | WORKING | Smoke test exercises a compact car through the full loop. |
| Civilian car roster | PARTIAL | Compact, sedan, sport, SUV, pickup and van assets/controllers; only compact car driving is regression-tested. |
| Police car and carjacking | PARTIAL | Police model, theft crime, displaced AI driver visual/NPC; police vehicle tactics remain simple. |
| Motorcycle | PARTIAL | Blender model and two-wheel controller; not separately road-tested. |
| Boat | PARTIAL | Spawn/entry and buoyancy-like water controller; spawn is smoke-tested, boating route is not. |
| Helicopter | PARTIAL | Blender model and arcade lift/forward controller; not separately flight-tested. |
| Airplane | PARTIAL | Blender model, runway spawn and speed-dependent lift/pitch; spawn is smoke-tested, takeoff/landing are not. |
| Drive-by shooting | PARTIAL | Aiming/fire path accepts pistol, heavy pistol and SMG from vehicles; no independent gameplay test or vehicle-specific presentation. |
| Vehicle health, impacts and explosions | PARTIAL | Collision damage, destruction effect, blast damage and repair; no deformation, fire spread or detailed damage zones. |
| Vehicle lights, horn, siren and audio | PARTIAL | Controls and generated cues exist; no per-vehicle audio/lighting QA. |
| Repair and vehicle customization | PARTIAL | Garage offers repair, six-color paint cycle and three engine/brake levels; no visual parts, wheels or stored collection. |
| Civilian traffic | PARTIAL | 18 initial moving cars follow fixed loops and move in smoke test; density/collision behavior is basic. |
| Traffic lights/intersections | PARTIAL | Visual signals and basic changing phases/stop-on-red behavior; no full right-of-way/intersection negotiation. |
| Lane changes, accident handling and public transit AI | NOT IMPLEMENTED | Cars do not change lanes or operate bus/train/taxi routes. |
| Pedestrian population and panic | PARTIAL | 32 initial walkers, replenishment, fleeing and distance pause; no traffic-aware crossing. |
| NPC visual variety | PARTIAL | Ordinary pedestrians choose among civilian, casual and business GLBs; police uses its own asset. No broader demographic/wardrobe system. |
| Pedestrian navigation mesh and crowd avoidance | NOT IMPLEMENTED | Direct waypoint steering; no NavigationRegion3D/NavigationAgent3D setup. |
| Police foot combat and tactical units | PARTIAL | Police pursue/attack; tougher tactical foot units appear at high stars, without cover/flanking. |
| Witness reporting | PARTIAL | Range/line-of-sight checks and delayed report by surviving pedestrian; no deeper dialogue/call behavior. |
| Wanted level 0–5 | WORKING | Heat thresholds, admin set/clear and police spawn are smoke-tested. |
| Pursuit, search and escape | PARTIAL | LOS loss enters search in smoke test; last-known location and timed heat decay exist, but pursuit tactics are basic. |
| Police roadblocks, helicopters and busted state | NOT IMPLEMENTED | No roadblock/air pursuit or arrest sequence. |
| Weapon shop and cash purchase | WORKING | SMG purchase spends cash and equips weapon in smoke test. |
| Hospital, safehouse and gas station | PARTIAL | Heal/armor, save/rest/outfit, and gas-station health/armor menus; no interiors or fuel system. |
| Broader economy and robbery | PARTIAL | Cash, prices and basic rewards exist; robbery, property and business loops absent. |
| Clothing/appearance | PARTIAL | Four outfit tints; no wardrobe, hair editor or clothing catalog. |
| Smartphone/personal device | NOT IMPLEMENTED | No phone UI or services. |
| Taxi passenger service and rail transit | NOT IMPLEMENTED | Admin `taxi` spawns a sedan only. |
| Birds and fish | PARTIAL | Distance-capped visual MultiMesh agents respond to gunfire; no collisions or wildlife interaction. |
| Day/night cycle | WORKING | Continuous clock and night lighting; day/night captures exist. |
| Weather | PARTIAL | Five presets, rain particles and fog/lighting adjustments; no wet-road physics or advanced storms. |
| HUD, minimap and full map | WORKING | Live status/map UI and GUI visual captures. |
| Benchmark/admin menu | WORKING | Teleport/spawn/wanted/weather/time/utility UI; smoke test verifies open/close and action paths. |
| Audio and world ambience | PARTIAL | Eight generated WAV files; ambience, footsteps, guns, engine, horn, siren and explosion have playback hooks. Rain WAV is currently unused; no radio/dialogue mix. |
| VFX | PARTIAL | Rain, muzzle/impact/tracer and explosion flashes; no decal, smoke or debris system. |
| Save/load | PARTIAL | Player state plus occupied vehicle position, damage, paint and upgrades restore in regression checks; other world entities do not persist. |
| Character skills | PARTIAL | Seven tracked values gain experience; no gameplay stat effects. |
| Shooting range, races, stunts and sports | NOT IMPLEMENTED | Optional activities were not added. |
| Simulation limits/optimization | PARTIAL | Population caps, distance checks and MultiMesh wildlife; no benchmarked FPS target or general streaming/LOD tiers. |
