# GPT-6 Sol — Unreal Engine 5 + Blender MCP GTA-Style Compact Open-World Benchmark

## Mission

You are the sole lead developer and technical artist of the **existing Unreal Engine 5 project in the currently opened Codex Desktop folder**.

Your task is to build the most complete and polished **GTA-style single-player open-world sandbox** you can achieve during this autonomous development session.

This is **not** a mission/story game. There must be **no campaign, no quests, no mission markers, no scripted story missions, and no progression gated behind missions**.

The game should launch directly into **free roam** and focus on emergent sandbox gameplay:
- exploring a compact open world on foot;
- stealing/entering/driving vehicles;
- cars, motorcycles, boats, helicopters and airplanes;
- using many weapon types;
- fighting NPCs and police;
- committing crimes that escalate a wanted/pursuit system;
- interacting with shops and useful world locations;
- swimming;
- vehicle damage and explosions;
- traffic and pedestrians;
- day/night and weather;
- unrestricted sandbox experimentation.

The experience should reproduce as many of the **general open-world sandbox capabilities associated with games such as GTA V** as practical, while being an **original game**.

Use **GTA V single-player as a mechanics/sandbox-interaction reference only**. Do not use it as an art-direction requirement. Do not copy GTA/RDR maps, code, missions, characters, names, logos, textures, audio, UI artwork, story content, proprietary assets, or ripped game files.

The final game must be an original project.

## Unreal Engine 5 implementation target

Use the existing **Unreal Engine 5** project and the exact installed stable UE5 version detected on this machine.

Technical defaults:

- **C++** is the primary runtime gameplay language.
- Use **Blueprints** where they improve iteration, animation, UI or designer-facing composition, but do not make the project depend on me manually wiring Blueprint graphs.
- Use **Unreal Editor Python**, Editor Utility workflows, commandlets and project-owned automation to create/configure assets and editor content reproducibly.
- **Enhanced Input** for player controls.
- **World Partition** only if it materially simplifies organization or performance for the compact map.
- **HLOD**, streaming sources, data layers and level-instance-style workflows where useful.
- **Mass Entity / Mass AI** is encouraged for dense pedestrian/traffic simulation if it materially improves scalability; otherwise use a simpler pooled Actor-based architecture.
- **Chaos Vehicles** / Chaos physics for vehicles and physical interactions where suitable.
- **Navigation System**, NavMesh, AI Controllers, Behavior Trees/State Trees, EQS or Mass processors where appropriate.
- **Niagara** for high-value VFX.
- **Lumen** for dynamic GI/reflections where practical.
- **Nanite** for suitable static environment assets where it improves quality/performance.
- **Virtual Shadow Maps** or another high-quality shadow solution appropriate to the detected UE version.
- **PCG Framework**, Splines, Geometry Script or Editor automation for environment dressing/roads where useful.
- **UMG/CommonUI-style** workflows for HUD/menus where appropriate.
- **Control Rig**, IK Rig/Retargeter, Animation Blueprints and Montages for characters/creatures where useful.
- Data Assets/Data Tables/Gameplay Tags for data-driven systems where they simplify the architecture.

Do not make the entire project Blueprint-only. Keep core runtime systems, data architecture, streaming, vehicle framework, wanted system, save system and heavy logic in clean C++ where practical.

Do not require paid marketplace plugins or paid asset packs.

---

# 0. Existing project and mandatory Blender connection

---

# 0. Existing project and mandatory Blender connection

Work directly in the current Unreal Engine 5 project containing the `.uproject` file. **Do not create another nested Unreal project.**

A Blender file for this Unreal Engine project already exists:

```text
UnrealGTAGPT6Sol.blend
```

The dedicated Blender MCP server for this project is:

```text
blender_unreal_gpt6sol
```

It connects to the Blender instance on:

```text
localhost:9881
```

## Critical Blender routing rule

For this project:

- use **only** the MCP server `blender_unreal_gpt6sol`;
- use the Blender instance on port **9881**;
- use `UnrealGTAGPT6Sol.blend` as the canonical Blender working file for this project;
- do **not** use `blender_unity_gpt6sol`;
- do **not** use `blender_godot_gpt6sol`;
- do not modify Blender files belonging to the other engine projects.

Before making major Blender changes, verify that the connected Blender scene is the intended Unreal GTA asset scene.

Keep `UnrealGTAGPT6Sol.blend` saved regularly.

You may create automatic backups or split-out source `.blend` files if genuinely necessary for stability, but `UnrealGTAGPT6Sol.blend` remains the master Blender scene for the Unreal Engine benchmark.

---

# 1. Blender MCP is mandatory

Blender MCP is a **required active part of the 3D asset-production pipeline**, not an optional extra.

Use `blender_unreal_gpt6sol` actively to:

- inspect the Blender scene;
- create meshes;
- edit geometry;
- create UVs;
- create materials;
- build rigs where useful;
- create basic animations where practical;
- position pivots/origins correctly;
- prepare LOD-friendly geometry;
- create collision helper meshes when useful;
- create render/viewport previews;
- inspect visible results;
- revise poor-looking assets;
- save source Blender work;
- export engine-ready assets;
- integrate them into Unreal Engine.

Do not merely write Blender Python scripts and leave them unused if the MCP can perform and verify the operation directly.

Do not ask me to manually model, UV, rig, export, import, or wire important assets.

## Blender visual QA loop

For every important hero asset:

1. inspect the current Blender scene;
2. model or modify the asset through `blender_unreal_gpt6sol`;
3. create appropriate materials/UVs;
4. inspect it visually using Blender viewport/render feedback when available;
5. revise it if it looks visibly broken, incoherent, or placeholder-quality;
6. save the Blender source;
7. export it in an engine-friendly format, preferably FBX for rigged/animated assets and FBX/GLB where appropriate for static assets;
8. import it into Unreal Engine;
9. configure materials, colliders, physics and gameplay components;
10. test it in the actual game.

A Blender asset is **not finished merely because a file exists**. It must work and look reasonable inside Unreal Engine.

---

# 2. No external asset shortcuts

This benchmark is about **GPT-6 Sol + Unreal Engine 5 + Blender MCP**.

Do not depend on:
- Unreal Marketplace/Fab asset packs;
- ripped GTA assets;
- Sketchfab;
- Poly Haven downloaded asset packs;
- Poly Pizza;
- Meshy;
- Rodin;
- Hunyuan 3D;
- external 3D generation services;
- copyrighted commercial asset packs;
- downloaded character/vehicle/weapon models.

Create the important project-specific 3D assets yourself with Blender MCP and project-owned code.

Built-in Unreal Engine functionality, built-in plugins, engine modules, and project-owned C++/Blueprint/Python content are allowed.

Procedurally generated project-owned textures, materials, meshes and audio are allowed.

---

# 3. Autonomy and workflow

I am an Unreal Engine beginner.

Do **not** expect me to manually:
- create levels, Actors, Blueprints, Components, or assets manually;
- assemble Blueprint classes or actor hierarchies manually;
- assign Unreal Editor Details-panel references manually;
- set up render settings;
- build UI;
- create materials;
- rig vehicles;
- configure weapons;
- set up NavMeshBoundsVolumes, AI Controllers, Behavior Trees, State Trees, Mass processors, or navigation data manually;
- create traffic routes;
- set up Blender exports;
- repair compile errors.

Take ownership of the implementation.

Before substantial work:

1. inspect the entire Unreal project and locate/read the `.uproject` file;
2. detect the exact installed Unreal Engine 5 version and project modules/plugins;
3. identify the current render pipeline;
4. identify installed packages;
5. inspect the existing `UnrealGTAGPT6Sol.blend`;
6. confirm access to `blender_unreal_gpt6sol`;
7. create a concise technical plan;
8. create/update `.gitignore`;
9. initialize Git and create a baseline commit if Git is available and the project is not already under version control;
10. create:
   - `DEVELOPMENT.md`
   - `FEATURE_MATRIX.md`
   - `SOL_FINAL_REPORT.md`
11. record a local session start timestamp.

Then work autonomously.

Do not stop and ask for confirmation after each subsystem.

If one feature becomes a deep blocker:
- preserve the working game;
- document the limitation;
- move to another high-value feature;
- return later if possible.

Do not claim an unfinished feature is working.

---

# 4. Overall design target

Create an original modern urban/crime open-world sandbox with:

- visual style chosen autonomously by GPT-6 Sol;
- third-person character control;
- optional first-person mode if practical;
- compact but dense open-world map of approximately 600×600 meters;
- varied urban and natural environments consistent with the chosen art direction;
- dense traffic and pedestrians;
- drivable land, air and water vehicles;
- combat;
- police response;
- wanted levels;
- shops;
- garages;
- weapon inventory;
- vehicle ownership/spawning for testing;
- day/night;
- weather;
- dynamic world reactions;
- explosions;
- physics;
- free-roam sandbox tools.

There must be **no mission system**.

The default experience is:

```text
Launch game
→ load/open world
→ control player immediately
→ freely explore and cause/interact with sandbox systems
```

---

# 5. Art direction — intentionally unspecified

You decide the visual style of this project.

There is **no prescribed visual style or 3D-model style**.

Do not infer an art direction from the fact that the gameplay is GTA-style.

Choose the visual style, character design language, vehicle design language, environment treatment, materials, lighting approach and 3D-model style entirely yourself based on what you believe will produce the strongest cohesive result within the available development time.

Once you choose an art direction:

- apply it consistently across the player, NPCs, vehicles, weapons, buildings, props, UI, VFX and world;
- use Blender MCP actively for important authored 3D assets;
- avoid accidental placeholder content unless it is temporary during implementation;
- prioritize visual coherence, readability and a finished-looking result over any particular style.

The GTA V reference in this prompt is about **mechanics, sandbox interactions and feature scope**, not about copying or imitating its graphics.
---

# 6. Unreal Engine 5 rendering and visual pipeline

Inspect the actual project settings first.

Unreal Engine does not determine the art direction; choose it yourself.

Use UE5 features only where they serve that direction and performance.

Available tools may include:
- Lumen;
- Nanite;
- Virtual Shadow Maps;
- atmospheric/volumetric fog;
- decals;
- post processing;
- material instances;
- Niagara;
- reflection systems;
- water systems;
- PCG and Geometry tools.

None of these are mandatory merely for visual prestige.

Build an efficient reusable material/lighting pipeline consistent with the style you choose and keep the game suitable for recording.
---

# 7. Compact open-world map — fixed target size

The playable map footprint should be approximately:

```text
600 m × 600 m
≈ 0.6 km × 0.6 km
≈ 0.36 km²
≈ 36 hectares
```

Treat this as a deliberate benchmark constraint.

Do **not** build an enormous open world and do not spend the session expanding the playable ground map beyond this approximate footprint.

The goal is to fit as much GTA-style sandbox density and mechanical variety as possible into a compact map.

## Map composition

Create a dense, varied map with several clearly distinguishable districts/areas inside the 600×600 m footprint.

Prioritize a compact combination of:

1. **Downtown / commercial core**
   - taller buildings;
   - shops;
   - intersections;
   - alleys;
   - parking;
   - traffic lights.

2. **Residential area**
   - houses/apartments;
   - smaller roads;
   - yards or courtyards;
   - a park or public space.

3. **Industrial / service area**
   - warehouse/factory-style buildings;
   - storage/service yards;
   - utility props;
   - garage/repair access.

4. **Waterfront / dock / beach edge**
   - enough water for swimming and boats;
   - piers/docks;
   - coastal road or promenade.

5. **Green / elevated area**
   - park, hillside, wooded area or other non-urban contrast;
   - paths and a viewpoint where practical.

6. **Compact aviation area**
   - helipad;
   - small airstrip/runway or equivalent test area if practical;
   - enough access to demonstrate helicopters and airplanes;
   - aircraft mechanics remain required even though the ground map is compact.
   - Aircraft may briefly fly beyond the main 600×600 m ground footprint over simplified/non-playable surroundings if needed for takeoff, landing and turning room, but do not expand the detailed playable ground map just for aviation.

7. **Road network**
   - interconnected roads;
   - at least one faster arterial/highway-like stretch;
   - intersections suitable for traffic and police pursuits;
   - route loops that make chases useful within the compact footprint.

Fit useful world locations into the same map where practical:
- police station;
- hospital/respawn point;
- weapon shop;
- vehicle customization/repair garage;
- safehouse;
- gas/service station;
- parking/vehicle access;
- clothing/appearance locations if implemented.

The world must be original and must not recreate Los Santos or any real GTA map.

Favor **density and interaction variety** over geographic scale.
---

# 8. Compact-world performance and simulation

The map is intentionally about 600×600 meters, so do not build heavyweight huge-world infrastructure merely for scale.

Still design the project to remain efficient with traffic, pedestrians, police, vehicles, wildlife, VFX, physics, interiors and shops.

Use sensible techniques available in the current engine, such as:
- pooling;
- distance-based AI update rates;
- LOD / visibility ranges / HLOD or equivalent when useful;
- occlusion/frustum culling;
- instancing for repeated props/vegetation;
- local activation/deactivation of expensive systems;
- spawn/despawn management around the player.

Sectoring/streaming may still be used if it simplifies organization or improves performance, but it is **not** a goal by itself.

Do not implement floating-origin systems or other huge-world infrastructure unless a real technical need appears.

The game should remain stable and suitable for screen recording.
---

# 9. Environment asset pipeline

Use Blender MCP to create **modular environment kits consistent with the art direction you choose** rather than individually hand-modeling every building from scratch.

Create reusable Blender collections/assets for:

### Buildings
- modern office façade modules;
- apartment modules;
- suburban house kit;
- storefront modules;
- industrial warehouse kit;
- airport/hangar kit;
- garage/parking structures.

### Street assets
- traffic lights;
- street lamps;
- traffic signs;
- barriers;
- benches;
- bins;
- fire hydrants;
- bus stops;
- fences;
- utility boxes;
- bollards;
- road cones.

### Roads
Use Unreal tooling/code (for example Splines, PCG, Editor Utility/Python automation, Geometry Script, and generated meshes) for road layout where more efficient, but create necessary visual assets through Blender:
- curbs;
- barriers;
- signs;
- lamp posts;
- road furniture.

Use project-generated materials/decals for:
- asphalt;
- lane markings;
- dirt;
- oil;
- cracks;
- crosswalks;
- sidewalks.

### Nature
Create/use project-owned:
- trees;
- bushes;
- rocks;
- grass;
- roadside vegetation.

Prioritize coherent silhouettes, reusable assets and sensible LODs.

---

# 10. Player character

Default gameplay should be **third-person**, similar to modern open-world action games.

For Unreal, prefer a robust C++ Character/Pawn controller with SpringArmComponent + CameraComponent for third-person camera behavior. Use Enhanced Input. Use CharacterMovementComponent where it fits, extending it for crouch, swimming, stealth, cover and traversal.

Implement:
- walk;
- run;
- sprint;
- jump;
- crouch if practical;
- aim;
- shooting;
- melee;
- weapon switching;
- entering/exiting vehicles;
- swimming;
- falling;
- ragdoll/death;
- camera orbit;
- camera collision;
- shoulder aiming;
- smooth movement transitions.

Optional/high priority:
- first-person mode toggle.

## Character model

Use Blender MCP to create an original player character in the visual style you choose for the project.

Do not leave the final player as an accidental engine placeholder unless that appearance is an intentional authored part of the chosen art direction.

Create the body, clothing/accessories and articulation appropriate to your chosen style.

Rig it for gameplay.

Use the current engine's character/skeleton/animation systems appropriately.

If fully bespoke animation is too expensive, prefer procedural animation, IK, reusable animation layers and carefully authored simple motion over visibly broken animation.

---


# 10A. Cover, stealth, traversal and close-combat depth

GTA V's on-foot sandbox is not only running and shooting. Build a more complete action-game movement/combat layer.

## Cover system — high priority

Implement a contextual cover system that works with common world geometry:

- enter/exit cover near suitable walls, barriers, vehicles and low objects;
- standing and low-cover poses;
- move left/right while attached to cover;
- peek around corners and over low cover;
- aim out from cover;
- fire from cover;
- blind fire with reduced accuracy;
- move around compatible cover corners;
- smoothly leave cover when movement demands it;
- avoid camera clipping while in cover.

Police and hostile AI should also use cover where practical.

## Stealth mode

Implement a GTA-like stealth state:

- slower/quieter movement;
- reduced footstep/noise radius;
- lower detection likelihood when out of sight;
- stealth takedown from behind;
- suppressed weapons interact with detection/noise;
- NPC suspicion/detection should depend on line of sight, distance, noise and obvious hostile behavior.

Stealth should remain a sandbox option and must not create stealth missions.

## Traversal

Improve basic traversal with:

- jump;
- climb/vault over low obstacles;
- contextual mantle where practical;
- ladder climbing;
- dodge/roll while aiming if practical;
- fall/landing reactions;
- ragdoll when impact thresholds are exceeded.

Do not turn traversal into parkour. Keep it grounded and GTA-like.

## Melee

Expand melee beyond a single attack:

- light/standard strike;
- heavier strike or contextual finisher;
- block/dodge where practical;
- melee weapon support;
- knockdown;
- different reactions based on attack direction/strength.

---

# 11. NPC population

Create a living population system.

NPC types should include:
- ordinary pedestrians;
- drivers;
- police officers;
- armed police/tactical units;
- shop staff;
- optional criminals/gang-like hostile NPCs.

NPCs should:
- walk around;
- use sidewalks;
- cross roads where practical;
- react to traffic;
- react to collisions;
- panic when gunfire/explosions occur;
- flee from danger;
- call/react to police events where practical;
- drive vehicles;
- fight or defend themselves depending on type;
- ragdoll or react physically to impacts;
- despawn/respawn intelligently for performance.

Create multiple visual variants through Blender:
- male/female body variations;
- clothing variations;
- skin/hair variations;
- police uniforms;
- tactical variants.

Do not make every NPC visually identical.

---

# 12. Traffic system

The city should contain believable civilian traffic.

For pedestrians, use Unreal Navigation, AI Controllers, Behavior Trees/State Trees or Mass AI where useful. Vehicle traffic should primarily follow a project-owned road/lane graph or spline network rather than forcing car traffic onto pedestrian navigation.

Implement:
- road graph / lanes;
- intersections;
- traffic signals;
- traffic direction;
- vehicle following;
- stopping;
- basic collision avoidance;
- lane changes where practical;
- traffic density scaling;
- vehicle spawn/despawn around player;
- parked vehicles;
- honking/reaction hooks;
- accidents and stopped traffic handling where practical.

Traffic AI does not need perfect real-world autonomy, but should look believable during normal driving.

---

# 13. Vehicle system

Vehicles are a major headline feature.

Implement a reusable vehicle framework with:
- enter/exit;
- driver seat;
- camera;
- steering;
- throttle;
- brakes;
- reverse;
- handbrake;
- suspension;
- tire grip;
- speed;
- engine state;
- damage;
- collision;
- headlights;
- brake lights;
- engine audio hooks;
- horn;
- vehicle health;
- explosions/fire when heavily damaged if practical.

Use Unreal's Chaos Vehicles / Chaos physics or another reliable project-owned vehicle implementation, with physically plausible suspension, tire forces, drivetrain and collision response.

## Vehicle classes

Create multiple distinct Blender-authored vehicles.

Minimum high-priority set:

### Cars
- compact car;
- sedan;
- sports car;
- muscle car;
- SUV;
- pickup;
- van;
- police cruiser.

### Two-wheel
- motorcycle;
- bicycle if practical.

### Water
- speedboat or motorboat.

### Air
- civilian helicopter;
- police helicopter;
- small propeller airplane;
- jet or fast airplane as stretch.

### Additional stretch
- truck;
- taxi;
- ambulance;
- fire truck;
- armored/police tactical vehicle.

Do not simply scale the same mesh to fake every vehicle category.

Use distinct silhouettes and handling profiles.

---


# 13A. Carjacking, drive-by combat and vehicle interaction

Vehicle gameplay must include the interactions that make a GTA-style sandbox feel complete.

## Carjacking

Support:

- entering an empty vehicle normally;
- pulling a civilian driver out of an occupied vehicle;
- NPC driver reaction/panic/fight-back depending on personality;
- theft of an occupied vehicle counting as a crime when witnessed;
- police vehicle theft causing an immediate serious response;
- motorcycles/bicycles using suitable contextual enter/steal animations.

The system may use simplified procedural transitions if full authored animations are not feasible, but it should not feel like instant teleportation when a visible character is in the seat.

## Drive-by shooting

Allow context-appropriate weapons while driving/riding:

- pistols;
- compact SMG-type weapons;
- throwable explosives where implemented;
- suitable one-handed weapons.

Implement:

- free aim from vehicle;
- camera support while aiming;
- reduced accuracy compared with standing fire;
- driver weapon restrictions;
- motorcycle forward/side firing;
- AI police/hostile drivers or passengers using vehicle weapons when appropriate.

## Vehicle detail interactions

Where practical support:

- headlights;
- high/low lights or simple toggle;
- brake lights;
- reverse lights;
- horn;
- siren for emergency vehicles;
- convertible roof on compatible cars;
- doors opening/closing;
- trunk/hood interaction as stretch;
- seat-specific entry points.

---

# 14. Vehicle Blender requirements

Major vehicles must be authored through `blender_unreal_gpt6sol`.

You decide the vehicle modeling style.

For each important vehicle, create the components required by the gameplay and chosen art direction, such as:
- body;
- wheels;
- lights;
- visible cabin/interior elements when useful;
- steering wheel where visible;
- separate wheel objects;
- correctly placed pivots/origins;
- appropriate scale;
- material slots;
- collision-friendly structure.

Create distinct silhouettes for different vehicle classes instead of only resizing one mesh.

Materials, geometry detail, proportions and surface treatment should follow the art direction you choose.

Create LOD-friendly or otherwise performance-conscious geometry where practical.

---


# 14A. Vehicle customization and repair shop — high priority

Create an original in-world vehicle modification/repair garage inspired by the general functionality of GTA V's car customization system.

It must not copy Los Santos Customs branding, UI, logos or shop design.

The player should be able to drive a compatible vehicle into the shop and:

## Repair
- restore vehicle health;
- repair broken glass/lights where represented;
- restore handling damage.

## Cosmetic customization
Support as many as practical:

- primary paint;
- secondary paint;
- gloss/matte/metallic-style finishes;
- wheels/rims;
- wheel color;
- window tint;
- bumpers;
- hood;
- roof options;
- grille;
- exhaust;
- skirts;
- spoiler;
- lights;
- horn;
- license plate style;
- optional livery/stripe variants.

## Performance customization
Make upgrades genuinely affect handling/performance:

- engine tuning;
- brakes;
- suspension;
- transmission;
- turbo/forced-induction-like upgrade;
- armor;
- tire durability / bullet-resistant tire option;
- spoiler/downforce tuning where appropriate.

Use data-driven upgrade definitions.

Visual body modifications should use Blender-authored modular parts or carefully designed mesh variants rather than simply changing scale.

The benchmark/debug menu may unlock everything immediately, but normal free-roam purchases should cost in-game money.

---

# 15. Aircraft and helicopter gameplay

## Helicopters

Implement:
- takeoff/landing;
- collective/lift;
- yaw;
- pitch/roll;
- forward flight;
- responsive arcade-realistic controls;
- third-person chase camera;
- damage/crash.

## Airplanes

Implement:
- throttle;
- takeoff;
- pitch;
- roll;
- yaw;
- stall/low-speed degradation at least approximately;
- landing;
- crash/damage.

Controls can be accessible/arcade-like rather than flight-simulator complex.

Airport runways must support meaningful flight gameplay.

---

# 16. Boats and swimming

Implement:
- swimmable water;
- player buoyancy/swim controls;
- drowning/health consequences if useful;
- boats with buoyancy-like motion;
- entering/exiting boats;
- coastal/port gameplay.

Water should visually fit the art direction you choose.

---

# 17. Weapons and combat

Create a substantial weapon system.

High-priority weapon categories:

### Melee
- fists;
- knife or melee weapon;
- bat/crowbar-like weapon.

### Handguns
- pistol;
- heavy pistol/revolver-like variant.

### SMG
- compact SMG.

### Shotguns
- pump shotgun;
- optional semi-auto shotgun.

### Rifles
- assault rifle;
- carbine;
- marksman rifle.

### Sniper
- sniper rifle with scoped aiming.

### Heavy/explosive
- grenade;
- rocket launcher;
- optional grenade launcher.

Do not copy GTA weapon models or brand names.

Use original generic weapon designs.

## Weapon behavior

Implement:
- weapon equip;
- weapon wheel or fast selection;
- ammo;
- magazines;
- reload;
- recoil;
- bullet spread;
- fire rate;
- hitscan/projectiles as appropriate;
- muzzle flash;
- shell/effect hooks;
- hit reactions;
- headshot/damage zones where practical;
- explosions;
- camera shake;
- aim zoom;
- scoped view for sniper.

Use Blender MCP to create proper 3D weapon models.

Do not use primitive cubes as final guns.

---


# 17A. Weapon customization and combat presentation

Create a weapon modification layer for compatible firearms.

At an original weapon shop/workbench, allow combinations such as:

- extended magazines;
- suppressor;
- flashlight;
- optic/scope;
- grip/stability upgrade where appropriate;
- cosmetic finish/tint;
- optional higher-capacity magazine for selected weapons.

Modifications must have actual gameplay effects when relevant:

- suppressor reduces audible detection radius but may slightly affect weapon balance;
- scope changes aiming/FOV;
- grip reduces recoil/spread;
- extended magazine increases capacity;
- flashlight illuminates dark environments while aiming.

## Ballistic interaction

Add practical material response:

- sparks on metal;
- dust/chips on concrete;
- glass breaking;
- decals or impact marks where performant;
- different impact audio hooks.

Stretch:
- limited penetration through thin materials;
- tire puncture from bullets;
- vehicle glass damage.

---

# 18. Police and wanted system — mandatory

This is one of the most important systems.

Implement a **0–5 level wanted/pursuit system**.

The player begins at:

```text
0 stars
```

Crimes increase wanted pressure.

Possible crime sources:
- attacking civilians;
- killing NPCs;
- shooting in public;
- attacking police;
- stealing occupied vehicles;
- major collisions;
- explosions;
- destroying police vehicles;
- continued resistance/arrests avoided.

Use witnesses/visibility where practical so not every crime instantly becomes globally known.

## Wanted levels

### 1 star
- nearby patrol responds;
- officers pursue/investigate.

### 2 stars
- more police cars;
- more aggressive pursuit;
- armed response.

### 3 stars
- larger vehicle response;
- roadblocks;
- police helicopter/search support if practical.

### 4 stars
- tactical/heavily armed units;
- stronger roadblocks;
- aggressive helicopter support.

### 5 stars
- maximum pursuit;
- many units;
- tactical vehicles/teams;
- sustained air support;
- difficult escape.

The exact numbers can be tuned for playability.

## Search / escape behavior

The wanted system should not simply be a timer.

Implement:
- police line of sight;
- last-known position;
- search radius/area;
- pursuing state;
- searching state;
- cooldown;
- stars decay when player remains unseen long enough.

Changing vehicle or hiding should help if police genuinely lose visual contact.

HUD must clearly show wanted level.

---


# 18A. Wanted-system refinements from GTA-style free roam

Strengthen the wanted system beyond simple star accumulation.

## Witnesses

Crimes should not always instantly become magically known.

When practical:

- civilians who directly witness a serious crime may panic and call police;
- if the player stops/escapes before a report completes, response may be delayed or prevented;
- gunshots/explosions can create an indirect report radius;
- police who directly see the crime report it immediately.

Do not make this system so complex that it destabilizes core police response.

## Active pursuit vs search state

Clearly separate:

### Pursuit
Police currently see/track the player.

### Search
Police have lost direct sight and search based on last known location.

During Search:
- wanted stars flash or otherwise visually communicate loss of direct contact;
- minimap shows nearby police/search directions or cones when practical;
- entering a police line of sight immediately resumes pursuit;
- escape timer progresses only while genuinely unseen.

Higher wanted levels should require a longer successful escape.

## Police vehicle tactics

Add GTA-like pursuit tactics where practical:

- PIT maneuver attempts;
- boxing-in at low speed;
- ramming from suitable angles;
- roadblocks at higher levels;
- spike strips as stretch;
- officers commandeering nearby civilian vehicles if stranded as stretch.

## Appearance / vehicle changes

During Search, changing obvious appearance or abandoning a clearly identified vehicle may slightly help the player evade detection.

Do not make it an instant universal wanted-clear button.

## Busted state

Police do not always need to kill the player.

If the player is:
- unarmed/not actively resisting;
- cornered at close range;
- or otherwise in an arrestable state,

allow a simplified **BUSTED/arrest** outcome.

On arrest:
- fade/respawn;
- clear wanted level;
- apply a modest money/ammo penalty if desired.

This should coexist with the death/respawn system.

---

# 19. Police AI

Police should:
- investigate crimes;
- pursue player;
- use vehicles;
- exit vehicles;
- aim/shoot when escalation warrants it;
- seek nearby positions/cover approximately;
- call/spawn reinforcements;
- create roadblocks at higher wanted levels;
- use helicopter support where implemented;
- attempt to surround/intercept rather than only drive directly into the player.

Do not spawn police directly in the player's visible field of view if avoidable.

---

# 20. Combat AI

Hostile NPCs should support:
- target selection;
- line of sight;
- firing;
- reloading;
- movement;
- basic cover use if practical;
- flanking/position changes as stretch;
- melee at close range where appropriate;
- death/ragdoll;
- surrender/fleeing behavior for civilians.

The player's combat should remain responsive even with many AI actors nearby.

---

# 21. Physics and reactions

Use physics to create convincing sandbox interactions.

Implement where practical:
- ragdolls;
- vehicle impacts;
- destructible light props;
- movable street objects;
- explosions applying force;
- NPC knockdown;
- vehicle-to-NPC impact reactions;
- damaged vehicle handling;
- broken street furniture;
- fire/explosion chain reactions for vehicles.

Do not attempt expensive full-building destruction if it compromises the project.

---

# 22. Vehicle damage

Vehicles should visibly and mechanically react to damage.

At minimum:
- health/durability;
- collision damage;
- smoke at heavy damage;
- fire before destruction if practical;
- eventual explosion/destruction;
- damaged handling.

Stretch:
- detachable doors/hood;
- broken glass;
- damaged lights;
- wheel damage;
- limited deformation.

A simple but convincing damage model is better than unstable full soft-body simulation.

---

# 23. World interactions

The player should be able to interact with the sandbox beyond shooting/driving.

High-priority:
- enter/exit vehicles;
- steal occupied vehicles;
- pick up weapons/ammo;
- buy/get weapons;
- use shops;
- interact with doors/use points;
- use garages/vehicle spawn points;
- swim;
- climb simple ladders if practical;
- use elevators/automatic doors where useful.

Optional:
- basic clothing change;
- vehicle repair;
- body armor purchase;
- food/health purchase.

No missions or quests.

---


# 23A. Player customization, safehouses, phone and personal services

Add free-roam identity/lifestyle systems that make the world feel closer to a complete GTA-style sandbox.

## Clothing

Create original clothing stores or wardrobe interaction with a modest but visible set of:

- shirts/jackets;
- pants;
- shoes;
- hats;
- glasses/accessories.

Use Blender-authored modular clothing or material variants where practical.

Clothing is cosmetic and must not become a mission requirement.

## Hair / appearance

Add a barber/salon-like interaction as stretch:

- hair variants;
- facial-hair variants;
- simple appearance options.

A tattoo shop is a lower-priority stretch feature.

## Safehouse

Provide at least one usable safehouse/home with:

- save/respawn role;
- wardrobe access;
- garage access;
- optional sleep/time-skip;
- basic interior.

## Garages / stored vehicles

Support storing a small set of player-selected vehicles persistently.

A stored/personal vehicle should be retrievable after loading when practical.

## Smartphone / interaction device

Create an original in-game phone or interaction interface for free-roam utility.

Useful functions may include:

- contacts/services;
- call/request taxi;
- camera/photo mode;
- quick save;
- vehicle/garage service;
- basic map/waypoint access.

Do not recreate GTA's exact phone UI or brands.

---

# 24. Economy and shops

Implement a lightweight free-roam economy.

Player has:
- cash balance;
- weapon shop purchases;
- ammo purchases;
- armor/health purchases where appropriate;
- vehicle repair or garage costs if implemented.

Because there are no missions, provide multiple sandbox-compatible ways to obtain/test money:
- initial reasonable cash;
- NPC cash drops where appropriate;
- pickups;
- debug/cheat controls;
- optional store robbery/emergent crime interaction as stretch.

Do not make economy grind block testing.

---

# 25. Weapon shop

Create at least one interactable weapon store.

It should allow buying:
- handguns;
- SMGs;
- shotguns;
- rifles;
- ammo;
- armor if implemented.

Use an original store identity and original UI.

---

# 26. Garage / vehicle access

Provide a convenient way to access vehicles for free-roam testing.

Possible implementations:
- garages;
- parking lots;
- dealership;
- debug/admin spawn menu.

The player must not be forced to search the whole map just to demonstrate a helicopter or airplane.

---

# 27. Day/night cycle

Implement:
- time-of-day cycle;
- sunrise;
- daylight;
- sunset;
- night;
- street lights;
- vehicle headlights;
- building/window lighting where practical;
- nighttime traffic/pedestrian variation if practical.

Visual quality at night matters.

---

# 28. Weather

Implement several weather states:

- clear;
- cloudy;
- rain;
- fog;
- storm if practical.

Weather should affect presentation:
- sky;
- lighting;
- wet-looking surfaces if possible;
- visibility;
- rain particles;
- reflections where supported.

Stretch:
- reduced vehicle grip in rain.

---

# 29. Map navigation

Implement a useful minimap/HUD navigation system.

At minimum:
- minimap or radar;
- player heading;
- roads or simplified map representation;
- police/wanted feedback;
- major world locations.

Stretch:
- full-screen map;
- waypoint;
- simple GPS route line.

No mission markers.

---


# 29A. Public transport, parachuting, underwater exploration and wildlife

These systems are high-value free-roam mechanics because they make the map feel like a world rather than a combat arena.

## Taxi passenger service

Allow the player to:

- hail/call a taxi;
- enter as a passenger;
- choose a map destination;
- ride there normally;
- optionally skip/fast-forward the trip after a short transition.

Taxi driving as a paid side activity is optional and should remain a free-roam activity, not a mission chain.

## Trains / transit

If technically practical, include:

- moving trains and/or subway;
- player can board/ride at least one transit type;
- trains follow fixed routes;
- collisions are handled safely.

## Parachute

Implement a usable parachute system:

- equip automatically or from inventory when available;
- deploy after jumping/falling;
- steer;
- descend;
- flare/slow for landing;
- landing damage for bad landings;
- altimeter while parachuting if useful.

Place parachutes at suitable high locations and/or aircraft so free-roam jumps are easy to test.

## Underwater / diving

Expand swimming with:

- diving below surface;
- breath/lung capacity;
- underwater camera/fog/audio treatment;
- deeper coastal areas;
- underwater props/wreck-like exploration areas;
- optional scuba gear that enables much longer underwater exploration.

## Wildlife

Populate suitable non-urban regions with a limited but convincing wildlife system.

Examples:
- deer;
- coyotes;
- boar;
- rabbits;
- birds;
- farm animals;
- dogs/cats;
- fish;
- sharks or another dangerous marine animal.

Animals should use simple ecosystem-appropriate behavior:
- grazing/wandering;
- fleeing;
- predators attacking only where appropriate;
- aquatic movement;
- region-based spawning.

Keep counts performance-conscious.

---

# 30. HUD

Create a polished GTA-like-but-original HUD.

Implement UI with UMG and project-owned widget classes/styles. CommonUI may be used if already available and it improves scalability. Avoid fragile hard-coded screen coordinates when anchors/layout containers are more appropriate.

Display as appropriate:
- health;
- armor;
- ammo;
- selected weapon;
- wanted stars;
- money;
- minimap;
- vehicle speed;
- vehicle health indicator if useful.

Do not copy GTA V's exact HUD artwork.

Create an original modern interface.

---

# 31. Weapon wheel

Implement a weapon-selection interface inspired by the general radial-selection concept used in modern action games.

It may:
- slow time while open;
- organize weapon classes;
- show ammo;
- allow quick mouse/controller selection.

Do not copy exact GTA V artwork.

---

# 32. Camera

Third-person camera should feel polished.

Implement:
- smooth follow;
- orbit;
- collision avoidance;
- aim camera;
- vehicle chase camera;
- vehicle look-around;
- helicopter/plane chase camera;
- camera shake;
- field-of-view tuning.

Optional:
- first-person toggle for player and vehicles.

---

# 33. Controls

Create sensible default keyboard/mouse controls.

Suggested defaults:

```text
WASD             Move / vehicle control
Mouse            Camera / aim
Left Shift       Sprint
Space            Jump / handbrake in vehicle depending context
Ctrl/C           Crouch if implemented
F                Enter/exit vehicle
Left Mouse       Fire / melee attack
Right Mouse      Aim
R                Reload
1-9 / wheel      Weapon selection
Tab              Weapon wheel
E                Interact
M                Map
Esc              Pause
V                Camera toggle
H                Horn where context-appropriate
```

Use context-sensitive controls where practical.

Add controller support only after keyboard/mouse is stable.

---

# 34. Audio

Do not use copyrighted GTA/RDR audio or music.

Use Unreal Audio Components, Sound Cues/MetaSounds, attenuation settings, submixes and generated/project-owned audio resources.

Create original/project-owned audio where practical:
- footsteps;
- gunshots;
- reloads;
- impacts;
- explosions;
- vehicle engines;
- tires/skids;
- horn;
- sirens;
- helicopter;
- ambient city;
- ocean;
- rain;
- UI.

If suitable audio generation is not possible with available tools, create simple original procedural/synthesized audio rather than copying commercial assets.

Audio quality is important, but do not block gameplay progress on perfect sound design.

---

# 35. Vehicle audio

Vehicles should have convincing feedback:
- engine pitch by RPM/speed;
- acceleration;
- braking/skid;
- impact;
- horn;
- police sirens;
- helicopter rotor;
- airplane engine.

Procedurally generated audio is acceptable.

---


# 35A. Emergency-world response and ambient services

Make the sandbox react to incidents beyond only police.

Where practical:

- ambulances respond to serious NPC injuries/deaths;
- fire trucks respond to major fires/explosions;
- emergency vehicles use sirens and traffic attempts to yield;
- responders despawn intelligently when far from the player;
- tow/service vehicle behavior is a stretch feature.

These systems are atmospheric. Do not let them overwhelm performance or interfere with the wanted system.

## Radio / in-vehicle media

Create an original radio/media system as a stretch feature.

Requirements:
- station/channel selection wheel or cycling;
- vehicle audio source;
- original/project-owned music or procedural audio only;
- no copyrighted commercial tracks;
- remember last selected station when practical.

A simple set of distinct original ambient stations is enough for the benchmark.

---

# 36. World ambience

Add environmental life:
- distant city noise;
- vehicle sounds;
- ambient pedestrian chatter-like nonverbal noise where possible;
- wind;
- coastal ambience;
- rain;
- industrial ambience.

Do not use copyrighted recordings.

---

# 37. Save/load

Implement basic persistence using Unreal's SaveGame framework and project-owned serializable data structures.

Save at least:
- player position;
- health/armor;
- money;
- owned/purchased weapons;
- ammo;
- current time;
- basic settings;
- optional last vehicle/garage state.

The game should recover gracefully if some dynamic world objects are not persisted.

---

# 38. Admin / benchmark menu — mandatory

Create a hidden or explicit benchmark/debug menu for rapid YouTube testing.

It must allow:
- teleport to key districts;
- spawn any implemented vehicle;
- spawn helicopter;
- spawn airplane;
- spawn boat;
- give any implemented weapon;
- refill ammo;
- set money;
- set health/armor;
- set wanted level 0–5;
- clear wanted level;
- spawn police;
- set time;
- set weather;
- repair current vehicle;
- open/test vehicle customization;
- unlock all vehicle modification options;
- give parachute;
- refill lung capacity / toggle scuba for testing;
- max/reset character skills;
- call taxi;
- toggle wildlife;
- toggle invulnerability;
- toggle traffic;
- toggle pedestrians;
- show FPS;
- show coordinates/sector.

This is a testing tool, not a mission system.

It should make it easy to demonstrate the whole project on video.

---

# 39. Blender project organization

Inside `UnrealGTAGPT6Sol.blend`, organize major assets into clear collections such as:

```text
GTA_UNREAL
  Characters
  NPCs
  Vehicles
    Cars
    Motorcycles
    Boats
    Helicopters
    Airplanes
  Weapons
  Buildings
  StreetProps
  Interiors
  Nature
  VFX_HelperMeshes
  CollisionHelpers
```

Use consistent naming.

Avoid thousands of unnamed objects such as:

```text
Cube.001
Cube.002
Cube.003
```

for final assets.

---

# 40. Unreal Engine asset/code organization

Organize project content clearly, for example:

```text
Source/
  GTA/
    Core/
    Player/
    Vehicles/
    Traffic/
    AI/
    Police/
    Weapons/
    Combat/
    World/
    Streaming/
    UI/
    Audio/
    Save/
    Debug/
    Editor/

Content/
  GTA/
    Characters/
    Vehicles/
    Weapons/
    World/
      Buildings/
      Roads/
      Props/
      Nature/
      Interiors/
    Materials/
    Textures/
    Animations/
    UI/
    VFX/
    Audio/
    Data/
    Generated/
    Maps/
```

Use Data Assets/Data Tables and Gameplay Tags where appropriate.

Use Blueprint child classes primarily as data/configuration/visual composition layers on top of reusable C++ gameplay classes.

Keep generated/automated content in predictable folders.

Adapt this structure to the existing project/module names rather than renaming everything unnecessarily.


---

# 41. Blender export pipeline

Create a reproducible Blender → Unreal Engine pipeline.

Preferred flow:

```text
UnrealGTAGPT6Sol.blend
→ Blender MCP edits
→ save source
→ export FBX/GLB as appropriate
→ SourceAssets/BlenderExports/... (or another clearly separated source-export folder)
→ Unreal Editor Python / automated import
→ /Game/GTA/Generated/...
→ material assignment
→ StaticMesh/SkeletalMesh configuration
→ Blueprint/Data Asset integration
→ gameplay integration
```

Preferred interchange:

- **FBX** for skeletal meshes, characters, rigged vehicles/parts and animation clips when reliable;
- FBX or glTF/GLB for static meshes based on which pipeline validates best in the installed UE5 build.

For static meshes:
- validate scale/orientation;
- generate/configure collision where appropriate;
- enable Nanite only when useful;
- create LODs only where still beneficial;
- validate material slots.

For skeletal meshes:
- preserve skeleton/armature;
- validate bone orientation;
- create/import animation clips when appropriate;
- configure Physics Asset where useful;
- configure IK Rig / Retargeter if needed.

Do not rely on manually importing every asset through the Content Browser.

Use Unreal Editor Python/C++ tooling to automate imports, paths, collision settings, material assignment and derived Blueprint creation where practical.

Do not depend on direct runtime use of the master `.blend` file. `UnrealGTAGPT6Sol.blend` is the editable source; exported files and imported Unreal assets are the runtime artifacts.


---

# 42. Materials and textures

Choose the material and texture treatment yourself as part of the art direction.

No material or texture style is prescribed. Choose the treatment yourself and keep it coherent with the rest of the game.

Create project-owned materials/textures that are coherent with the style you choose.

Useful material categories may include:
- road/asphalt-like surfaces;
- concrete/stone-like surfaces;
- building façades;
- glass;
- metal;
- vehicle body materials;
- tires;
- clothing;
- skin/hair or stylized equivalents;
- wood;
- vegetation;
- dirt/sand/rock;
- water;
- emissive signs/lights.

Use Blender materials, generated textures and the engine's native material/shader system as appropriate.

Keep the pipeline efficient and reusable. Use atlases, trim sheets, material instances/variants or shared shader resources when they materially help.

Do not copy copyrighted GTA/RDR texture assets.
---

# 43. LODs, Nanite, HLOD and instancing

Create an appropriate scalable rendering strategy for:
- vehicles;
- buildings;
- trees;
- street props;
- NPCs.

Use:
- Nanite for suitable high-detail static environment assets;
- HLOD for World Partition cells/large environment groupings;
- LODs for skeletal meshes, vehicles and assets where Nanite is not appropriate;
- Instanced Static Mesh / Hierarchical Instanced Static Mesh components for repeated props/vegetation;
- distance culling where useful.

Do not render every high-detail asset at full cost across the entire city.


---

# 44. Lighting

Choose the lighting style yourself as part of the overall art direction.

The lighting system should:
- make gameplay readable;
- support day/night;
- support street/building/vehicle lights;
- support police/emergency lights;
- support interiors where implemented;
- support weather changes;
- remain performant during recording.

Use the current engine's lighting, fog, shadow, reflection and post-processing features as appropriate for the style you choose.

Do not impose a visual target beyond the art direction you choose.
---

# 45. VFX

Create useful effects for:
- muzzle flashes;
- bullet impacts;
- sparks;
- dust;
- explosions;
- fire;
- smoke;
- vehicle damage;
- rain;
- water splashes;
- tire smoke;
- skid effects;
- helicopter dust;
- police lights.

Use Unreal-native VFX systems such as:
- Niagara;
- Decals;
- Material Parameter Collections;
- dynamic lights where appropriate;
- camera shake;
- post-process feedback.

Keep common effects pooled/reusable and performance-conscious.


---

# 46. Interaction quality

Add feedback so systems feel intentional:
- enter vehicle animation/transition;
- weapon equip;
- reload;
- recoil;
- hit reactions;
- vehicle impact feedback;
- police sirens;
- screen feedback for low health;
- subtle camera effects;
- pickup feedback;
- UI sounds.

Keep UI feedback consistent with the art direction and gameplay needs.

---


# 46A. Character skill progression

Add a lightweight use-based character-stat system inspired by GTA V's single-player attributes.

High-value skills:

- **Stamina** — improves sustained sprinting/cycling/swimming.
- **Shooting** — modestly improves recoil control/reload handling.
- **Strength** — improves melee performance and physical resilience.
- **Stealth** — improves quiet movement/detection profile.
- **Driving** — improves difficult vehicle control/recovery slightly.
- **Flying** — reduces turbulence/handling penalties and improves aircraft control slightly.
- **Lung Capacity** — increases underwater breath time.

Skills should improve primarily by doing the relevant activity.

Keep benefits noticeable but not so strong that low-skill gameplay feels broken.

Expose current stats in a pause/status screen or debug menu.

The system is free-roam progression and must not create mission prerequisites.

---

# 47. World activities — but NO missions

Do not implement missions.

Allowed free-roam activities/interactions include:
- driving;
- flying;
- boating;
- shooting;
- police chases;
- shops;
- garage/vehicle spawning;
- swimming;
- exploration;
- stunts;
- emergent traffic accidents;
- fighting;
- changing time/weather through debug tools;
- sandbox destruction.

Do **not** add:
- mission start markers;
- quest objectives;
- NPC quest givers;
- story cutscenes;
- campaign progression;
- scripted heists;
- mission rewards.

---


# 47A. Optional hobbies and free-roam activities

GTA V contains many activities that are not part of the core story. Add a **small selection only after the primary sandbox is stable**.

These are optional stretch goals and must not become quests/missions.

Good candidates:

- shooting range;
- street race;
- off-road race;
- sea race;
- time trial;
- parachute/base-jump challenge;
- triathlon;
- golf putting/driving-range style activity;
- tennis;
- darts;
- stunt jumps.

Implementation rule:

- activities begin only when the player deliberately interacts with an activity point;
- no story dialogue/campaign progression;
- simple score/time/reward is acceptable;
- do not spend core-development time implementing all of them before police, traffic, vehicles and combat are stable.

## Shooting range

Highest priority among activities because it reuses the combat system.

Support:
- timed target sequences;
- score;
- accuracy;
- optional small cash reward;
- contributes to Shooting skill.

## Stunt jumps

Place optional stunt locations around the map.

Track:
- successful jump;
- distance/airtime;
- safe landing.

This adds exploration value without requiring missions.

---

# 48. Difficulty and player survivability

Implement:
- health;
- armor;
- damage;
- fall damage;
- vehicle collision damage;
- bullets;
- explosions;
- drowning if appropriate;
- death/respawn.

On death:
- fade/death UI;
- respawn at a sensible location/hospital-like respawn point;
- reset wanted level;
- preserve basic sandbox access.

---

# 49. Performance targets

The project must remain playable.

Avoid:
- one ticking Actor/Component per tiny prop where unnecessary;
- thousands of unpooled traffic/NPC objects;
- expensive AI for distant entities;
- rendering all interiors;
- heavy per-frame reflection updates everywhere;
- all vehicles/NPCs simulating across the full map;
- unbounded rigidbodies.

Use:
- pooling;
- distance-based simulation tiers;
- LOD;
- streaming;
- simplified distant AI;
- culled interiors;
- sensible traffic/pedestrian caps.

Prefer stable 30–60 FPS at reasonable recording settings over theoretical ultra graphics that are unusable.

---

# 50. AI simulation tiers

Use simulation levels:

### Near player
Full:
- animation;
- physics;
- driving;
- combat;
- reactions.

### Medium distance
Reduced:
- simplified AI;
- limited updates.

### Far distance
Represented minimally or despawned.

This is especially important for:
- pedestrians;
- civilian traffic;
- police units.

---

# 51. Priority order

This project is extremely broad.

Use this implementation priority:

1. stable Unreal Engine project;
2. player + camera;
3. compact 600×600 m world foundation;
4. one polished Blender-authored car + enter/drive/exit;
5. traffic foundation;
6. player weapon/combat foundation;
7. NPC/pedestrian foundation;
8. police + wanted system;
9. broaden vehicle roster;
10. complete, dense and varied compact map;
11. helicopters/airplanes/boats;
12. broaden weapon roster;
13. vehicle customization/repair;
14. shops/economy/player customization;
15. cover/stealth/drive-by polish;
16. day/night/weather;
17. better NPC reactions and witness behavior;
18. parachute/diving/wildlife/public transport;
19. vehicle damage/explosions;
20. art-direction/material/lighting polish pass;
21. character-stat progression;
22. selected free-roam activities;
23. additional world interaction;
24. benchmark/debug menu;
25. final QA and optimization.

Do **not** stop at step 4.

Continue adding working breadth for as long as the session allows.

---

# 52. Definition of a strong first milestone

A strong first milestone requires:

- player can walk in third person;
- the compact 600×600 m map is playable and meaningfully populated;
- at least one finished Blender-authored car exists;
- player can enter/drive/exit it;
- basic traffic exists;
- at least one finished Blender-authored firearm exists;
- player can shoot;
- pedestrians exist;
- police can respond;
- wanted stars work;
- basic UI works.

Once this is stable, continue expanding toward the full scope.

---

# 53. Do not fake breadth

Do not populate menus with 30 vehicles if only one actually works.

A feature may be marked:

```text
WORKING
PARTIAL
NOT IMPLEMENTED
```

Only mark `WORKING` if it has a functional in-game path.

For decorative-only assets, label them clearly.

---

# 54. QA workflow

Continuously:
- build the C++ project;
- inspect UnrealBuildTool/compiler output;
- inspect Unreal logs;
- fix C++/Blueprint/material/shader/import errors;
- load the main map;
- test gameplay;
- verify imported Blender assets;
- verify scale/orientation;
- verify collision;
- verify materials;
- verify SkeletalMesh/PhysicsAsset/animations;
- check broken asset references;
- validate World Partition streaming behavior.

Find the installed Unreal Engine path automatically if possible.

Use Unreal automation when helpful:
- Visual Studio/MSBuild/UnrealBuildTool;
- `UnrealEditor-Cmd.exe`;
- Unreal Editor Python;
- commandlets;
- project-owned automation scripts;
- automation tests where practical.

Validate:
- C++ Editor target builds;
- required plugins load;
- maps open;
- Blueprint assets compile;
- shaders/materials compile;
- generated/imported assets resolve;
- project default map/game mode are valid.

Do not wait until the very end to discover that the project does not build or open.


---

# 55. Blender QA

Before accepting major assets:
- inspect silhouette;
- inspect proportions;
- inspect normals;
- inspect materials;
- check scale;
- check origin;
- check transforms;
- remove accidental duplicate geometry;
- check wheel pivots;
- check weapon grip/origin;
- check character rig orientation;
- check object names.

Then validate the exported result inside Unreal Engine.

---

# 56. Final Unreal Engine validation

Before declaring completion:

1. the `.uproject` is healthy;
2. the C++ Editor target builds;
3. required plugins/modules load;
4. the configured default map opens;
5. player can move;
6. the intended compact map areas are accessible;
7. World Partition/streaming behaves acceptably;
8. traffic works;
9. pedestrians work;
10. vehicles work;
11. weapon combat works;
12. police response works;
13. 0–5 wanted system works;
14. player can lose wanted level through real search/LOS behavior;
15. cover and stealth work if marked WORKING;
16. drive-by shooting works if marked WORKING;
17. vehicle modification/repair works if marked WORKING;
18. parachute/diving/taxi/wildlife systems behave correctly if marked WORKING;
19. helicopter or airplane works if marked WORKING;
20. shops work if marked WORKING;
21. admin menu works;
22. Blender-generated assets import/display correctly;
23. no major missing materials/assets;
24. save/load works at least at basic level;
25. there is no major Blueprint compile-error spam;
26. performance is acceptable for recording.


---

# 57. Required documentation

Maintain:

## `DEVELOPMENT.md`
Include:
- Unreal Engine version, enabled plugins, and rendering configuration;
- render pipeline;
- project architecture;
- controls;
- systems;
- scene layout;
- Blender asset workflow;
- build/run instructions;
- known limitations.

## `FEATURE_MATRIX.md`

Create a table:

```text
Feature | Status | Notes
```

Include all major systems from this prompt.

Use only:
- `WORKING`
- `PARTIAL`
- `NOT IMPLEMENTED`

Be honest.

## `SOL_FINAL_REPORT.md`

At completion include:
- what was built;
- what works;
- what is partial;
- known bugs;
- controls;
- build result;
- major assets created in Blender;
- vehicle roster;
- weapon roster;
- map regions;
- wanted/police implementation;
- performance notes.

---

# 58. Session timing, token accounting and API-equivalent cost — mandatory

These metrics will be shown in the YouTube comparison between the three engine versions, so collect them carefully and **do not fabricate precision**.

## 58.1 Development time

At the very beginning of the task, before substantial development work:

1. Read the current local timestamp from the operating system.
2. Save it to:
   ```text
   .sol-run/start_time.txt
   ```
3. Preserve that timestamp for the final report.

At the end of the task:

1. Read the current local timestamp again.
2. Save it to:
   ```text
   .sol-run/end_time.txt
   ```
3. Calculate the full wall-clock elapsed time.

The timer includes:
- planning;
- coding;
- Blender MCP work;
- model creation;
- exports/imports;
- Unreal C++ build, asset import, shader compilation, map loading and runtime validation;
- testing;
- debugging;
- asset generation;
- waiting for builds/imports/commands during this autonomous session.

Report:
- start timestamp;
- end timestamp;
- elapsed time as `HH:MM:SS`;
- elapsed minutes;
- elapsed hours as a decimal if useful.

Do not estimate development duration from memory if timestamps are available.

## 58.2 Token usage

At the end, make a best effort to obtain **real token usage for this Codex/GPT-6 Sol session**.

Preferred sources, in order:

1. session/runtime usage telemetry exposed directly by Codex;
2. Codex session status/usage data;
3. the current Codex session log/JSONL under the user's Codex data directory if accessible;
4. another official local session-usage source.

Identify the correct current session carefully using:
- working directory;
- timestamps;
- session ID;
- recent activity.

Where available, report separately:

- input tokens;
- cached input tokens;
- cache-write tokens;
- output tokens;
- reasoning tokens if reported separately;
- total tokens.

Important:
- determine whether usage records are **per-turn** or **cumulative** before adding them;
- do not double-count cumulative snapshots;
- do not count the same cached/cache-write tokens twice;
- do not invent token numbers merely to fill the report.

If exact token usage is unavailable, write exactly:

```text
Exact token usage unavailable from accessible runtime.
```

You may provide a clearly labeled estimate only if there is a defensible data source and method.

## 58.3 GPT-6 Sol API-equivalent price

Calculate the approximate **token-only API-equivalent cost in USD** for the work performed in this session.

This is **not** the user's actual ChatGPT/Codex subscription charge. It is a benchmark estimate answering:

> "What would this token usage have cost at GPT-6 Sol API token rates?"

Use the **current official GPT-6 Sol API pricing from an authoritative OpenAI source** if that pricing is accessible at runtime.

Record:
- the source/rates used;
- the token categories used in the calculation;
- whether cached tokens have distinct pricing;
- whether long-context multipliers or other pricing rules applied.

Do not reuse GPT-6 GPT-6 Sol rates.

Do not invent GPT-6 Sol pricing if official pricing cannot be retrieved.

If exact official pricing is unavailable, write:

```text
GPT-6 Sol API-equivalent cost unavailable because authoritative pricing could not be verified.
```

If token telemetry is incomplete, clearly label any calculable amount as an estimate and explain the limitations.

### Tool fees

If Blender MCP, local tools, the game engine, or other local tool calls do not have a separately metered API fee, do not invent one.

If an external paid tool/API somehow becomes involved despite the project rules, report its fee separately rather than hiding it inside token cost.

Label the main number:

```text
GPT-6 Sol token-only API-equivalent cost
```
## 58.4 Required metrics in `SOL_FINAL_REPORT.md`

At the bottom of `SOL_FINAL_REPORT.md`, include exactly this section:

```text
## GPT-6 Sol Session Metrics

Start:
End:
Elapsed:
Elapsed minutes:
Elapsed hours:

Model:
Reasoning effort (if available):

Input tokens:
Cached input tokens:
Cache-write tokens:
Output tokens:
Reasoning tokens (if separately available):
Total tokens:

GPT-6 Sol token-only API-equivalent cost:
Pricing rates/source used:
Long-context adjustment:
External tool/API fees included: No/Yes
Confidence / data source:

Blender MCP server used: blender_unreal_gpt6sol
Blender port: 9881
Blender master file: UnrealGTAGPT6Sol.blend
Approximate number of major Blender assets created:
```

The final response to me must also explicitly state:

1. total development time;
2. total token usage, if available;
3. GPT-6 Sol token-only API-equivalent price in USD, if calculable;
4. whether the figures are exact or estimated.

These metrics are mandatory for the benchmark and should be calculated only after the final validation pass.

---

# 59. Git / recovery

Use Git where practical.

Create meaningful milestone commits, for example:
- baseline;
- player;
- first drivable car;
- combat;
- police/wanted;
- world expansion;
- air vehicles;
- final stabilization.

Do not commit:
- `Binaries/`;
- `DerivedDataCache/`;
- `Intermediate/`;
- `Saved/`;
- packaged build outputs;
- large generated caches;
- other standard Unreal transient data.

Do not allow an experimental change to destroy the only working state.

---

# 60. Final asset-quality gate

Before final completion, visually inspect the game and reject **unintentional placeholder content**.

Important visible content should not remain as raw engine primitives or temporary debug assets unless that is a deliberate, coherent part of the art direction you chose.

Prioritize authored/finalized versions of:
1. player character;
2. main vehicles;
3. police;
4. common pedestrians;
5. weapons;
6. important buildings/landmarks;
7. street props.

Any intentionally chosen visual style is acceptable if it is applied consistently.

The problem is unfinished placeholder presentation, not any specific art style.

Document any remaining placeholder or incomplete assets honestly.
---

# 61. Final response

Do not finish by only saying "done".

Before responding:

1. run final validation;
2. fix critical parse/runtime errors;
3. inspect `FEATURE_MATRIX.md`;
4. update `SOL_FINAL_REPORT.md`;
5. save `UnrealGTAGPT6Sol.blend`;
6. ensure exported Blender assets are imported into the Unreal project and load cleanly;
7. confirm the game launches into free roam;
8. confirm there are **no missions**;
9. confirm the admin/benchmark menu provides rapid access to implemented systems;
10. make a final Git commit if the repository is healthy.

Then provide a concise final summary with:

- how to launch;
- controls;
- map regions;
- vehicle list;
- weapon list;
- wanted/police features;
- Blender assets created;
- what remains incomplete;
- elapsed development time;
- token usage if available;
- GPT-6 Sol token-only API-equivalent cost in USD if calculable;
- whether the reported metrics are exact or estimated.

---

# 62. Start now

Begin immediately.

First:
- inspect the existing Unreal Engine project;
- verify `blender_unreal_gpt6sol`;
- verify `UnrealGTAGPT6Sol.blend`;
- record start time;
- assess render pipeline;
- create the implementation plan.

Then build the game autonomously.

Remember:

**This is a missionless GTA-style free-roam sandbox on a compact approximately 600×600 meter map. The visual style is your decision.**

**Unreal Engine 5 is the game engine.**

**`blender_unreal_gpt6sol` on port 9881 and `UnrealGTAGPT6Sol.blend` are the mandatory Blender asset pipeline.**

**Use Blender actively, not merely as an optional export tool.**
