# Harborline feature matrix

`WORKING` means a functional in-game path was confirmed by the available build or smoke check. `PARTIAL` means code or scene content exists, but the requested behavior is incomplete or has not yet been verified interactively. The final editor audit found 24 scene vehicles, 25 actors, 9 services, 6,851 renderers and no issues. A clean Windows batch-mode standalone smoke with virtual Input System devices exited with code 0 and no logged exceptions: `S` moved the player 2.78 m, Civilian 18 moved 1.28 m, `F1` opened, a pistol shot spent one round (15→14), isolated JSON save/load restored cash/ammo, wanted level 3 activated pursuit, a car was entered, and rain was selected. Two traffic vehicles and a port van moved in the five-second check. The driven sports car traveled 65.09 m in four seconds, crossed `z=-80`, held a 180° heading and retained full 180/180 health; speed at the end of held `W` was 32.86 m/s. RenderTexture captures show the boulevard, beach, park, airfield and imported models. The clean batch run reports HUD/admin UI images as `UNAVAILABLE_BATCH`; two earlier hidden non-batch launches intermittently stalled during graphics/input initialization. Manual input, UI appearance, longer driving quality and performance need visible play testing. See `.sol-run/scene_audit.txt` and `.sol-run/standalone_clean.log`.

| Feature | Status | Notes |
| --- | --- | --- |
| Existing Unity project and URP setup | WORKING | Unity 6000.6.0f1, URP 17.6 and Input System; no nested project. |
| Editor scene generation and build settings | WORKING | Builder generated `Harborline.unity`; it is the enabled build scene. |
| Windows standalone build | WORKING | `.sol-run/player_build_virtual_input.log` reports a successful player build; batch-mode standalone ran. |
| Clean startup without runtime exceptions | PARTIAL | Clean virtual-input batch smoke exited with code 0 and no logged exceptions; two earlier hidden non-batch attempts intermittently stalled during graphics/input initialization. |
| Direct missionless free roam | WORKING | Standalone smoke loaded the sandbox scene; no mission or quest system is present. |
| Original art direction and source assets | PARTIAL | Cohesive coastal palette and original Blender models appear in region RenderTexture captures; final visible-window quality review remains. |
| Dedicated Blender MCP authoring and master source | WORKING | Port 9879 master `.blend` was edited and saved through the dedicated MCP connection; 38 FBX exports exist. |
| Blender to Unity model integration | PARTIAL | Builder imports and places FBX models visible in in-game RenderTexture captures; interaction/collision and complete asset review remain. |
| Rigged player and authored animations | PARTIAL | A rigged FBX with idle/walk actions exists; default gameplay uses segmented procedural limb motion, not a validated Animator rig. |
| Player and NPC visual variants | PARTIAL | Original player, two civilian, police and tactical FBX models exist; repetition and in-game appearance remain unreviewed. |
| Map footprint and district layout | PARTIAL | Generated scene spans about 600 × 600 m; boulevard, beach, park and airfield RenderTexture views were captured, while full traversal remains untested. |
| Roads, curbs, crosswalks and routes | PARTIAL | Grid, harbor road, runway and six loops are generated; audit found zero curb blockers and the final scripted car crossed `z=-80` cleanly. Longer routes need QA. |
| Buildings, service landmarks and storefronts | PARTIAL | Distinct Blender building kit and nine locations are placed; models appear in region captures, while facade access and collisions need play review. |
| Street props and nature kit | PARTIAL | Authored lights, signals, stop, benches, hydrant, containers and trees plus generated details; many requested prop types are absent. |
| Waterfront, docks, beach and water | PARTIAL | Water plane, seabed, piers and marina boat exist; swimming and boat access are untested. |
| Airfield and helipad | PARTIAL | Runway, helipad, hangar and aircraft spawn points exist; usable takeoff/landing not yet demonstrated. |
| Active LOD groups and occlusion setup | NOT IMPLEMENTED | No authored `LODGroup` or explicit occlusion pass found. |
| Distance based AI and population limits | PARTIAL | Pedestrians throttle distant updates and traffic stops far away; pooling and full simulation tiers are absent. |
| Third person walk, run, sprint, jump, crouch | PARTIAL | Virtual Input System `S` moved the player 2.78 m; manual movement feel and other actions remain untested. |
| Procedural character locomotion | PARTIAL | Runtime shoulder/hip pivots animate segmented arms and legs; motion is not visually verified in Unity. |
| Camera orbit, shoulder aim and collision | PARTIAL | Follow, orbit, spherecast avoidance and aim FOV exist; mouse feel and collision need testing. |
| First person camera toggle | PARTIAL | `V` toggle exists; character and vehicle first person presentation has not been reviewed. |
| Cover system | PARTIAL | `Q` wall detection, lateral movement and exit exist; peeking, corner transitions and blind fire are missing. |
| Stealth and detection | PARTIAL | `Z` slows movement and exposes a noise radius; AI detection does not yet use full stealth/LOS/noise rules or takedowns. |
| Vaulting, ladders, dodge and mantle | NOT IMPLEMENTED | Basic jump/fall exists; contextual traversal actions are absent. |
| Melee depth | PARTIAL | Fists and baton can hit; block, heavy strike, knockdown and directional reactions are absent. |
| Swimming and drowning | PARTIAL | Surface swimming, dive input, lungs and drowning damage exist; in-game water entry remains untested. |
| Underwater exploration and scuba | PARTIAL | Breath/Scuba toggles exist; underwater visuals, audio and exploration props are not complete. |
| Parachute | PARTIAL | `P` deploy, steering, slower descent, flare and procedural canopy exist; flight and landing need play testing. |
| Health, armor, fall damage, death and respawn | PARTIAL | Damage and delayed respawn code exists; death/respawn presentation and all damage sources are not verified. |
| Civilian pedestrians and shop staff | PARTIAL | Smoke found 31 actors and Civilian 18 moved 1.28 m; wander, panic and shopkeeper roles exist, but sidewalks/crossings/reactions are basic. |
| Police and tactical NPC combat | PARTIAL | Officers chase on foot, sight player and shoot at higher wanted levels; cover/flanking/arrest are absent. |
| Witness reporting and crime detection | PARTIAL | Civilian delayed reports, police direct reports and gunshot radius exist; witness interruption and full crime visibility need testing. |
| Civilian vehicle traffic | PARTIAL | Pickup 04 and Port Van 08 each moved 48.02 m; SportsCar 02 moved 24.42 m but was at 0 kph after five seconds, without damage. Ten scene traffic vehicles follow loops; longer route behavior needs review. |
| Lane graph, traffic signals and road rules | PARTIAL | Visual signals and loop waypoints exist; signal phase, lane changes and robust intersection rules are absent. |
| Parked vehicles and vehicle spawn access | PARTIAL | Parked roster and admin spawn list exist; `F1` opened by injected key, but buttons were not clicked or visually confirmed. |
| Car driving physics | PARTIAL | Virtual `W` drove a sports car 65.09 m in four seconds, crossing `z=-80` at 180° heading with full 180/180 health; 32.86 m/s at the end of held input. Longer/manual driving remains untested. |
| Motorcycle riding | PARTIAL | Blender model and balancing physics exist; real handling and entry need testing. |
| Boat handling | PARTIAL | Blender motorboat, buoyancy and throttle/steer exist; water operation is untested. |
| Helicopter flight | PARTIAL | Model, rotor, lift/yaw/forward controls and damage exist; takeoff/landing not verified. |
| Airplane flight | PARTIAL | Model, propeller, throttle, lift/stall approximation and pitch/roll/yaw exist; takeoff/landing not verified. |
| Vehicle roster breadth | PARTIAL | Several car silhouettes plus motorcycle, boat, helicopter and plane exist; requested sedan/muscle/SUV/bicycle/jet breadth is incomplete. |
| Enter and exit vehicles | PARTIAL | Smoke confirmed scripted entry into one car; manual `F`, exit placement and all vehicle classes need testing. |
| Carjacking occupied vehicles | PARTIAL | AI occupancy can be replaced and theft triggers a crime event; driver ejection, reaction and transition animation are absent. |
| Drive by shooting | PARTIAL | Sidearms/SMG/grenade are allowed in vehicles with extra spread; free aim and AI passengers are untested or absent. |
| Vehicle lights, sirens and animated parts | PARTIAL | Head/brake/police lights and named wheel/rotor/propeller pivots are wired; night and emergency behavior need visual checks. |
| Vehicle damage, fire and explosions | PARTIAL | Collision/gun health, damaged handling hooks, smoke and explosion event exist; fire staging/deformation are limited. |
| Vehicle repair and customization | PARTIAL | Garage UI offers repair, three paint colors and engine/brake/armor upgrades with physical effects; body part catalog is absent. |
| Weapon inventory and switching | PARTIAL | Eleven weapon definitions, number keys and cycling exist; hand controlled selection is untested. |
| Blender weapon models | PARTIAL | Pistol, SMG, shotgun and carbine FBX models are imported; several listed weapons lack distinct finished models. |
| Shooting, reload, recoil and projectiles | PARTIAL | Injected left click fired a pistol shot and reduced ammo 15→14; aiming feel, hits, reload and other weapons need play testing. |
| Weapon customization | PARTIAL | Suppressor, grip, extended magazine and scope data/effects exist; no complete purchase/workbench flow or attachment visuals. |
| Ballistic material response | PARTIAL | Impact particles and vehicle/world color variation exist; glass, tires and material specific effects are absent. |
| Wanted level 0–5 | PARTIAL | Smoke confirmed setting level 3 with active pursuit; full crime escalation and 0–5 play behavior are untested. |
| Pursuit, search, LOS and escape | PARTIAL | Last known position, police raycast sight and unseen decay exist; full search behavior and escape need testing. |
| Police reinforcements and vehicle pursuit | PARTIAL | Officers and emergency cruisers spawn for wanted levels; driving tactics and pursuit behavior need play testing. |
| Roadblocks, PIT, helicopter pursuit | NOT IMPLEMENTED | No implemented roadblock, PIT, spike strip or active police air support path. |
| Busted/arrest state | NOT IMPLEMENTED | Death/respawn exists; no arrest outcome. |
| Ragdolls and knockdown | NOT IMPLEMENTED | Dead NPCs are disabled/tilted; no physics ragdoll. |
| Explosive physics and prop destruction | PARTIAL | Explosions damage actors/vehicles and push rigidbodies; light street furniture and chain reactions are limited. |
| Weapon shop and cash economy | PARTIAL | Purchase UI, starting cash, ammo/armor and NPC cash awards exist; latest run has no HUD startup exception, but shop transactions remain untested. |
| Safehouse and hospital | PARTIAL | Save, sleep, health restoration and respawn role exist; interior and wardrobe access are absent. |
| Fuel station and garage services | PARTIAL | Interactable fuel repair and garage menu exist; actual UI operation was not smoke tested. |
| Clothing, barber and appearance | NOT IMPLEMENTED | Clothing storefront marker exists without a wardrobe/appearance workflow. |
| Stored personal vehicles and phone | NOT IMPLEMENTED | No persistent garage storage or original phone service interface. |
| Day/night cycle and lighting | PARTIAL | Sun, ambient, street and vehicle lights respond to hour; night quality is unreviewed. |
| Weather | PARTIAL | Clear/cloudy/rain/fog/storm states and rain particles exist; smoke selected Rain, visual result is unconfirmed. |
| Minimap and full map | PARTIAL | HUD radar and full map panels are coded; no startup exception in the clean run, but batch UI images were unavailable and no route guidance exists. |
| HUD, pause and weapon wheel | PARTIAL | Health, armor, ammo, cash, wanted, speed, menu and wheel are coded; the clean smoke has no HUD exception, but batch UI capture was unavailable and appearance remains unverified. |
| Admin/benchmark menu | PARTIAL | Injected `F1` opened the menu state; teleports, spawns and other buttons remain unclicked, and batch UI capture was unavailable. The 720p panel layout is in the rebuilt player but needs visual review. |
| Procedural SFX and vehicle audio | PARTIAL | Project owned tones, impacts, engines, horn/siren hooks and smoke exist; ambient mix and playback quality need listening QA. |
| World ambience, emergency services, radio | NOT IMPLEMENTED | No complete ambient soundscape, ambulance/fire response or station/media system. |
| Save/load persistence | PARTIAL | Isolated JSON round trip restored cash and pistol ammo after mutation; broader state and dynamic world persistence remain untested/incomplete. |
| Character skill progression | PARTIAL | Seven stats persist and some actions raise counters; gameplay benefits and all use based gains are incomplete. |
| Taxi, transit and wildlife | NOT IMPLEMENTED | Admin wildlife toggle has no animals; no rideable taxi/train or ecosystem. |
| Optional free roam activities | NOT IMPLEMENTED | No shooting range, races, stunts or other scored activity loop. |
| Performance and recording readiness | PARTIAL | World RenderTexture views are visible, but no measured FPS or sustained recording test exists; batch UI images were unavailable and two earlier non-batch startups stalled. |
| Final Unity visual and interaction QA | PARTIAL | Clean batch smoke exited with code 0 and no logged exceptions; virtual pedestrian/player movement, traffic movement, firing, save/load, F1, car entry and a clean 65.09 m crossing work. Manual controls, UI visuals, longer handling and sustained play remain. |
