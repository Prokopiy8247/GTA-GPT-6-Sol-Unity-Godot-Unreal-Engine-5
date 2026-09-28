# Harborline Blender asset inventory

The canonical source is [UnityGTAGPT6Sol.blend](UnityGTAGPT6Sol.blend). It was authored in Blender 5.2.2 LTS through a dedicated `mcp-for-blender` stdio connection to the Unity project instance at `localhost:9879`. The source currently contains **38 authored asset roots**, and the Unity project contains **38 matching FBX exports** in [Assets/GTA/Generated/Models](Assets/GTA/Generated/Models). No downloaded models or texture packs were used.

## Models

| Category | FBX | Description |
| --- | --- | --- |
| Vehicle | `HL_Car_CoralRunner.fbx` | Warm coral civilian sedan; four wheel pivots. |
| Vehicle | `HL_Car_PoliceInterceptor.fbx` | Cobalt patrol sedan with lightbar; four wheel pivots. |
| Vehicle | `HL_Car_DocksidePickup.fbx` | Cream utility pickup with open bed; four wheel pivots. |
| Vehicle | `HL_Car_SprintGT.fbx` | Low cobalt sports car with rear wing; four wheel pivots. |
| Vehicle | `HL_Van_Delivery.fbx` | High roof cargo van; four wheel pivots. |
| Vehicle | `HL_Motorcycle_Tide.fbx` | Compact teal motorcycle; front/rear wheel pivots. |
| Vehicle | `HL_Boat_Lagoon.fbx` | Small outboard motorboat. |
| Vehicle | `HL_Helicopter_Seabird.fbx` | Light helicopter; `Rotor_Main` and `Rotor_Tail` pivots. |
| Vehicle | `HL_Plane_CoastSkimmer.fbx` | Single engine airplane; `Propeller` pivot. |
| Character | `HL_Player_Nova.fbx` | Static segmented player model used as a safe fallback. |
| Character | `HL_Player_Nova_Rigged.fbx` | Separate 12 bone, 17 weighted part player with `HL_Nova_Idle` and `HL_Nova_Walk` actions. |
| Character | `HL_Pedestrian_01.fbx` | Teal outfit pedestrian with skirt. |
| Character | `HL_Pedestrian_02.fbx` | Cobalt jacket pedestrian. |
| Character | `HL_Police_Officer.fbx` | Patrol uniform with hat and badge. |
| Character | `HL_Police_Tactical.fbx` | Dark tactical police variant. |
| Weapon | `HL_Pistol_Wave9.fbx` | Compact pistol with separate slide, grip and sights. |
| Weapon | `HL_Carbine_Breakwater.fbx` | Cobalt and gunmetal carbine. |
| Weapon | `HL_Shotgun_Breaker.fbx` | Pump shotgun with wood stock. |
| Weapon | `HL_SMG_Current.fbx` | Compact submachine gun. |
| Building | `HL_Building_BayMarket.fbx` | Warm concrete and cobalt market storefront. |
| Building | `HL_Building_Apartment.fbx` | Four floor apartment block with balconies. |
| Building | `HL_Building_Warehouse.fbx` | Industrial warehouse and loading area. |
| Building | `HL_Building_CoastalHouse.fbx` | Pitched roof coastal residence. |
| Building | `HL_Building_PoliceStation.fbx` | Civic police station with tower, glazed lobby and radio aerial. |
| Building | `HL_Building_Clinic.fbx` | Clinic with entrance canopy and medical symbol. |
| Building | `HL_Building_RepairGarage.fbx` | Three bay repair garage and forecourt. |
| Building | `HL_Building_FuelStation.fbx` | Fuel kiosk, canopy and two dispensers. |
| Building | `HL_Building_CoastHangar.fbx` | Aviation hangar with door facade and apron. |
| Street / nature | `HL_Streetlight_Harbor.fbx` | Tall waterfront lamp with warm diffuser. |
| Street / nature | `HL_TrafficLight.fbx` | Three lamp intersection signal. |
| Street / nature | `HL_BusStop_Coast.fbx` | Glass backed shelter with bench. |
| Street / nature | `HL_Bench_Promenade.fbx` | Driftwood and steel bench. |
| Street / nature | `HL_Hydrant_Coral.fbx` | Coral fire hydrant. |
| Street / nature | `HL_Container_Dock.fbx` | Corrugated teal shipping container. |
| Street / nature | `HL_Tree_Canopy.fbx` | Broad layered canopy tree. |
| Street / nature | `HL_Tree_Palm.fbx` | Coastal palm. |
| Street / nature | `HL_Prop_Helipad.fbx` | 10.5 m landing pad with H marking. |
| Street / nature | `HL_Prop_PromenadePier.fbx` | 17 m timber pier with pylons and railing. |

## Units and placement

- Blender uses metric units, 1 Blender unit = 1 metre, Z up. The export scripts request FBX Y up and +Z forward for Unity.
- Vehicles, people and building entrances face Blender `-Y`, which becomes Unity `+Z` after export. Building fronts and interaction triggers belong on the Unity `+Z` side.
- Asset roots export at `(0, 0, 0)` even though the source scene lays them out in rows for inspection. Building and prop roots are at ground level; vehicle roots are near ground or water level.
- `HL_Prop_PromenadePier` starts at its shoreward end and extends 17 m in Unity `+Z`; its deck is about 1.35 m above its root. The helipad root is at the disc centre.
- Static FBX meshes have named Blender material slots. Unity material mapping, colliders, Rigidbody components, lighting and gameplay logic are configured by the Unity project rather than embedded in FBX.

## Blender source

The assets were authored through a dedicated Blender MCP workflow. The public project includes the saved `UnityGTAGPT6Sol.blend` master file and all exported FBX models. Machine-specific automation, screenshots and session files are excluded because they are not required to open, run, or rebuild the Unity project.

## Current limits

- The default pedestrian and police FBX variants are segmented static meshes. The Unity runtime can animate their named body parts procedurally; they are not skinned FBX rigs.
- `HL_Player_Nova_Rigged.fbx` is a separate candidate. Its rigid 100% segment weights and simple idle/walk keyframes were checked in Blender, but Unity import and Animator wiring must be validated before using it as the default visual.
- Vehicle wheel, rotor and propeller pivots are present, while actual steering, suspension and propulsion belong to Unity gameplay components.
- Building doors, hangar shutters, windows and shop fronts are visible geometry. These models do not contain finished interiors or functional door animations.
- The MCP addon reported protocol 7 while the client expected 9. The required scene inspection, code execution, viewport screenshots, source saves and FBX exports nevertheless succeeded through compatibility fallbacks.
