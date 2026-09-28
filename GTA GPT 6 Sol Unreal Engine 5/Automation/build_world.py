"""Build the 600 x 600 m Harbor City level in the existing Unreal project.

Run in Unreal Editor Python after import_assets.py and after the game module
compiles. The layout data in world_layout.json is also consumed by gameplay.
This script is intentionally rerunnable: it clears actors in HarborCity and
rebuilds the entire authored environment with stable asset and actor names.
"""

import json
from pathlib import Path
import random
import unreal


ROOT = Path(unreal.Paths.project_dir())
LAYOUT = json.loads((ROOT / "Automation" / "world_layout.json").read_text(encoding="utf-8"))
LEVEL = LAYOUT["level"]
ASSET_ROOT = "/Game/GTA/Generated"
MATERIAL_ROOT = "/Game/GTA/Materials"
ENGINE_CUBE = "/Engine/BasicShapes/Cube.Cube"
ENGINE_SPHERE = "/Engine/BasicShapes/Sphere.Sphere"
RNG = random.Random(632791)
MATS = {}
MESHES = {}
ACTOR_COUNT = 0


def color(r, g, b):
    return unreal.LinearColor(r, g, b, 1.0)


PALETTE = {
    "ground": (0.39, 0.39, 0.34, 0.96, 0.0),
    "asphalt": (0.065, 0.086, 0.105, 0.92, 0.0),
    "road_edge": (0.17, 0.22, 0.23, 0.91, 0.0),
    "sidewalk": (0.44, 0.49, 0.47, 0.83, 0.0),
    "line": (0.96, 0.83, 0.57, 0.76, 0.0),
    "graphite": (0.075, 0.105, 0.13, 0.72, 0.08),
    "graphite_light": (0.15, 0.21, 0.24, 0.70, 0.07),
    "teal": (0.035, 0.53, 0.53, 0.67, 0.08),
    "coral": (0.84, 0.30, 0.22, 0.72, 0.0),
    "sand": (0.68, 0.55, 0.38, 0.87, 0.0),
    "cream": (0.83, 0.77, 0.63, 0.79, 0.0),
    "glass": (0.045, 0.19, 0.245, 0.20, 0.35),
    "water": (0.017, 0.23, 0.30, 0.18, 0.38),
    "water_glint": (0.16, 0.60, 0.62, 0.16, 0.30),
    "grass": (0.14, 0.32, 0.25, 0.97, 0.0),
    "grass_light": (0.26, 0.43, 0.31, 0.98, 0.0),
    "rust": (0.49, 0.23, 0.18, 0.88, 0.0),
    "warm_light": (0.95, 0.68, 0.33, 0.42, 0.0),
}


def make_material(name, values):
    asset_path = MATERIAL_ROOT + "/M_" + name.title().replace("_", "")
    if unreal.EditorAssetLibrary.does_asset_exist(asset_path):
        existing = unreal.EditorAssetLibrary.load_asset(asset_path)
        if existing:
            return existing
    tools = unreal.AssetToolsHelpers.get_asset_tools()
    mat = tools.create_asset(asset_path.rsplit("/", 1)[-1], MATERIAL_ROOT,
                             unreal.Material, unreal.MaterialFactoryNew())
    if mat is None:
        raise RuntimeError("Cannot create material " + asset_path)
    base = unreal.MaterialEditingLibrary.create_material_expression(
        mat, unreal.MaterialExpressionVectorParameter, -540, -160)
    base.set_editor_property("parameter_name", "BaseTint")
    base.set_editor_property("default_value", color(*values[:3]))
    unreal.MaterialEditingLibrary.connect_material_property(
        base, "", unreal.MaterialProperty.MP_BASE_COLOR)
    rough = unreal.MaterialEditingLibrary.create_material_expression(
        mat, unreal.MaterialExpressionScalarParameter, -540, 80)
    rough.set_editor_property("parameter_name", "SurfaceRoughness")
    rough.set_editor_property("default_value", values[3])
    unreal.MaterialEditingLibrary.connect_material_property(
        rough, "", unreal.MaterialProperty.MP_ROUGHNESS)
    metal = unreal.MaterialEditingLibrary.create_material_expression(
        mat, unreal.MaterialExpressionScalarParameter, -540, 280)
    metal.set_editor_property("parameter_name", "SurfaceMetallic")
    metal.set_editor_property("default_value", values[4])
    unreal.MaterialEditingLibrary.connect_material_property(
        metal, "", unreal.MaterialProperty.MP_METALLIC)
    unreal.MaterialEditingLibrary.recompile_material(mat)
    unreal.EditorAssetLibrary.save_loaded_asset(mat)
    return mat


def load_materials():
    unreal.EditorAssetLibrary.make_directory(MATERIAL_ROOT)
    for name, values in PALETTE.items():
        MATS[name] = make_material(name, values)


def load_meshes():
    MESHES["cube"] = unreal.EditorAssetLibrary.load_asset(ENGINE_CUBE)
    MESHES["sphere"] = unreal.EditorAssetLibrary.load_asset(ENGINE_SPHERE)
    for name in ("Building", "Streetlamp", "TrafficLight", "Tree", "Bench",
                 "Warehouse", "House", "Office", "Container", "Dock", "Rock"):
        path = ASSET_ROOT + "/SM_" + name
        MESHES[name] = (unreal.EditorAssetLibrary.load_asset(path)
                        if unreal.EditorAssetLibrary.does_asset_exist(path) else None)
    for required in ("Building", "Streetlamp", "TrafficLight"):
        if not MESHES[required]:
            unreal.log_warning("HARBOR: imported SM_{} absent; using generated geometry".format(required))
    if not MESHES["cube"] or not MESHES["sphere"]:
        raise RuntimeError("Engine BasicShapes meshes unavailable")


def v(x, y, z):
    return unreal.Vector(float(x), float(y), float(z))


def rotation(yaw=0, pitch=0, roll=0):
    # UE Python positional Rotator parameters are Roll, Pitch, Yaw. Named
    # parameters avoid pitching PlayerStart when only a heading was requested.
    return unreal.Rotator(pitch=float(pitch), yaw=float(yaw), roll=float(roll))


def actor_label(actor, label):
    global ACTOR_COUNT
    ACTOR_COUNT += 1
    actor.set_actor_label("HC_" + label)
    return actor


def mesh_actor(label, mesh, position, scale=(1, 1, 1), yaw=0, material=None,
               collision=True):
    if not mesh:
        return None
    actor = unreal.EditorLevelLibrary.spawn_actor_from_class(
        unreal.StaticMeshActor, v(*position), rotation(yaw))
    actor_label(actor, label)
    comp = actor.get_editor_property("static_mesh_component")
    comp.set_static_mesh(mesh)
    actor.set_actor_scale3d(v(*scale))
    if material:
        comp.set_material(0, MATS[material] if isinstance(material, str) else material)
    if not collision:
        # Set the serialized profile as well as the live collision state.
        # SetCollisionEnabled alone became "Custom" and reloaded as
        # QueryAndPhysics in UE 5.8, leaving the visual sea surface solid.
        comp.set_collision_profile_name("NoCollision")
        comp.set_collision_enabled(unreal.CollisionEnabled.NO_COLLISION)
        actor.set_actor_enable_collision(False)
    return actor


def box(label, position, size, material, collision=True, yaw=0):
    return mesh_actor(label, MESHES["cube"], position,
                      (size[0] / 100.0, size[1] / 100.0, size[2] / 100.0),
                      yaw, material, collision)


def sphere(label, position, size, material, collision=True):
    return mesh_actor(label, MESHES["sphere"], position,
                      (size[0] / 100.0, size[1] / 100.0, size[2] / 100.0),
                      0, material, collision)


def imported(label, mesh_name, position, desired_size=None, yaw=0):
    mesh = MESHES.get(mesh_name)
    if not mesh:
        return None
    scale = (1, 1, 1)
    location = list(position)
    if desired_size:
        try:
            bounds = mesh.get_bounds()
            ext = bounds.box_extent
            origin = bounds.origin
            scale = tuple(float(desired_size[i]) / max(2.0 * float(a), 1.0)
                          for i, a in enumerate((ext.x, ext.y, ext.z)))
            location[2] -= (float(origin.z) - float(ext.z)) * scale[2]
        except Exception as exc:
            unreal.log_warning("HARBOR mesh bounds unavailable for {}: {}".format(mesh_name, exc))
    return mesh_actor(label, mesh, location, scale, yaw)


def tag(actor, values):
    if actor:
        try:
            actor.set_editor_property("tags", [unreal.Name(s) for s in values])
        except Exception:
            actor.set_editor_property("tags", values)


def prepare_level():
    unreal.EditorAssetLibrary.make_directory("/Game/GTA/Maps")
    if unreal.EditorAssetLibrary.does_asset_exist(LEVEL):
        unreal.EditorLevelLibrary.load_level(LEVEL)
        for actor in list(unreal.EditorLevelLibrary.get_all_level_actors()):
            if isinstance(actor, unreal.WorldSettings):
                continue
            try:
                unreal.EditorLevelLibrary.destroy_actor(actor)
            except Exception:
                pass
    else:
        if not unreal.EditorLevelLibrary.new_level(LEVEL):
            raise RuntimeError("Could not create level " + LEVEL)


def build_ground_and_water():
    # 600 m x 600 m exact playable footprint. The sea floor is lower than land.
    box("Land_600m", (0, 5500, -100), (60000, 49000, 200), "ground")
    box("WaterFloor", (0, -24500, -700), (60000, 11000, 200), "sand")
    box("SeaSurface", (0, -24500, -35), (60000, 11000, 10), "water", False)
    try:
        water_volume = unreal.EditorLevelLibrary.spawn_actor_from_class(
            unreal.PhysicsVolume, v(0, -24500, -240))
        actor_label(water_volume, "WaterPhysicsVolume")
        water_volume.set_actor_scale3d(v(300, 55, 3))
        water_volume.set_editor_property("water_volume", True)
        water_volume.set_editor_property("fluid_friction", 0.45)
        tag(water_volume, ["HarborCity.Water"])
    except Exception as exc:
        unreal.log_warning("HARBOR: PhysicsVolume water setup: " + str(exc))
    box("WaterfrontPromenade", (0, -18400, 18), (60000, 1200, 36), "sand")
    box("Seawall", (0, -19000, -40), (60000, 130, 160), "graphite")
    for i, x in enumerate(range(-27500, 29000, 2800)):
        box("SeaGlint_{:02d}".format(i), (x, -22500 - (i % 4) * 1450, -28),
            (980, 25, 4), "water_glint", False)
    # Pier decks leave room below for water gameplay and a boat approach.
    for p, x in enumerate((-9500, 4500)):
        box("PierDeck_{}".format(p), (x, -21300, 90), (1200, 5100, 90), "sand")
        for side in (-1, 1):
            for j, y in enumerate((-19100, -21200, -23300)):
                box("PierPost_{}_{}_{}".format(p, side, j),
                    (x + side * 500, y, -170), (90, 90, 550), "graphite")
    marina = unreal.EditorLevelLibrary.spawn_actor_from_class(
        unreal.TargetPoint, v(0, -18000, 100))
    actor_label(marina, "POI_Marina")
    tag(marina, ["HarborCity.POI", "HarborCity.Marina"])


def build_roads():
    xs = LAYOUT["vertical_roads_x"]
    ys = LAYOUT["horizontal_roads_y"]
    vertical_sidewalk_spans = ((-18000, -17100), (-14900, -4900),
                               (-3100, 7100), (8900, 19100), (20900, 30000))
    horizontal_sidewalk_spans = ((-30000, -24900), (-23100, -12900),
                                 (-11100, -1100), (1100, 11100),
                                 (12900, 23100), (24900, 30000))
    for i, x in enumerate(xs):
        width = 2000 if x == 0 else 1600
        box("RoadV_{}".format(i), (x, 6000, 7), (width, 48000, 14), "asphalt")
        for side in (-1, 1):
            for segment, (start, end) in enumerate(vertical_sidewalk_spans):
                box("SidewalkV_{}_{}_{}".format(i, side, segment),
                    (x + side * (width / 2 + 270), (start + end) / 2, 19),
                    (540, end - start, 34), "sidewalk")
        for j, y in enumerate(range(-17200, 29200, 2600)):
            box("LaneV_{}_{}".format(i, j), (x, y, 16),
                (13, 880, 2), "line", False)
    for i, y in enumerate(ys):
        width = 2000 if y == -16000 else 1600
        box("RoadH_{}".format(i), (0, y, 9), (60000, width, 18), "asphalt")
        for side in (-1, 1):
            for segment, (start, end) in enumerate(horizontal_sidewalk_spans):
                box("SidewalkH_{}_{}_{}".format(i, side, segment),
                    ((start + end) / 2, y + side * (width / 2 + 270), 22),
                    (end - start, 540, 34), "sidewalk")
        for j, x in enumerate(range(-29000, 30000, 2600)):
            box("LaneH_{}_{}".format(i, j), (x, y, 19),
                (880, 13, 2), "line", False)
    # Short clear crosswalks at the four main civic intersections.
    for ix, x in enumerate((-12000, 0, 12000, 24000)):
        for iy, y in enumerate((-4000, 8000, 20000)):
            for stripe in range(-2, 3):
                box("Crosswalk_{}_{}_{}".format(ix, iy, stripe),
                    (x + stripe * 270, y - 1450, 21),
                    (130, 620, 3), "cream", False)


def streetlamp(label, x, y, yaw=0):
    if not imported(label, "Streetlamp", (x, y, 22), (230, 230, 720), yaw):
        box(label + "_Post", (x, y, 350), (42, 42, 700), "graphite")
        box(label + "_Head", (x + 75, y, 690), (190, 70, 50), "warm_light", False)
    if int(label.rsplit("_", 1)[-1]) % 2 == 0:
        point = unreal.EditorLevelLibrary.spawn_actor_from_class(
            unreal.PointLight, v(x, y, 640))
        actor_label(point, label + "_Light")
        try:
            comp = point.get_editor_property("light_component")
            comp.set_editor_property("intensity", 4500.0)
            comp.set_editor_property("attenuation_radius", 1700.0)
            comp.set_editor_property("cast_shadows", False)
            comp.set_editor_property("light_color", unreal.Color(255, 184, 111, 255))
        except Exception as exc:
            unreal.log_warning("HARBOR street light setup: " + str(exc))


def traffic_light(label, x, y, yaw=0):
    if imported(label, "TrafficLight", (x, y, 22), (150, 150, 550), yaw):
        return
    box(label + "_Post", (x, y, 250), (35, 35, 500), "graphite")
    box(label + "_Signal", (x, y, 490), (95, 80, 180), "coral", False)


def build_streetscape():
    for i, x in enumerate(LAYOUT["vertical_roads_x"]):
        for j, y in enumerate(range(-13500, 28000, 8500)):
            if any(abs(y - road_y) < 1200 for road_y in LAYOUT["horizontal_roads_y"]):
                continue
            streetlamp("LampV_{}_{}".format(i, j), x + 1250, y, 180)
    for i, y in enumerate(LAYOUT["horizontal_roads_y"]):
        for j, x in enumerate(range(-27000, 29000, 8500)):
            if any(abs(x - road_x) < 1200 for road_x in LAYOUT["vertical_roads_x"]):
                continue
            streetlamp("LampH_{}_{}".format(i, j), x, y - 1300, 90)
    for i, x in enumerate(LAYOUT["vertical_roads_x"]):
        for j, y in enumerate(LAYOUT["horizontal_roads_y"]):
            traffic_light("Signal_{}_{}_A".format(i, j), x - 1150, y - 1150)
            traffic_light("Signal_{}_{}_B".format(i, j), x + 1150, y + 1150, 180)


def procedural_building(label, x, y, width, depth, height, style, accent):
    box(label + "_Core", (x, y, height / 2 + 20),
        (width, depth, height), style)
    box(label + "_Roof", (x, y, height + 45),
        (width + 110, depth + 110, 90), accent)
    floor_count = min(11, max(2, int(height / 950)))
    for floor in range(floor_count):
        z = 420 + floor * (height - 740) / max(floor_count - 1, 1)
        for side in (-1, 1):
            box(label + "_Window_{}_{}".format(floor, side),
                (x + side * (width / 2 + 4), y, z),
                (12, depth * 0.70, 175), "glass", False)
    box(label + "_Entrance", (x, y - depth / 2 - 8, 195),
        (width * 0.28, 15, 390), "warm_light", False)


def accessible_interior(label, x, y, width, depth, height, accent, tag_name,
                        front="south"):
    # The marker is outside a walkable front door. Garage has a wider door.
    half = width / 2
    wall = 45
    door_width = 650 if tag_name == "Garage" else 130
    box(label + "_Floor", (x, y, 35), (width, depth, 70), "sand")
    direction = -1 if front == "south" else 1
    box(label + "_Back", (x, y - direction * depth / 2, height / 2),
        (width, wall, height), "graphite_light")
    for side in (-1, 1):
        box(label + "_Side_{}".format(side),
            (x + side * half, y, height / 2), (wall, depth, height), "graphite_light")
        section = (width - door_width) / 2
        box(label + "_Front_{}".format(side),
            (x + side * (section / 2 + door_width / 2),
             y + direction * depth / 2, height / 2),
            (section, wall, height), accent)
    box(label + "_Canopy", (x, y + direction * (depth / 2 + 180), height - 125),
        (width + 240, 340, 90), accent)
    box(label + "_Roof", (x, y, height + 40),
        (width + 140, depth + 140, 80), "graphite")
    box(label + "_Counter", (x + 850, y + 300, 140),
        (1100, 230, 280), "sand")
    for i in range(3):
        box(label + "_Shelf_{}".format(i),
            (x - half + 260, y - 750 + i * 750, 275),
            (290, 480, 550), "teal")
    light = unreal.EditorLevelLibrary.spawn_actor_from_class(
        unreal.PointLight, v(x, y, height - 200))
    actor_label(light, label + "_InteriorLight")
    try:
        comp = light.get_editor_property("light_component")
        comp.set_editor_property("intensity", 3600.0)
        comp.set_editor_property("attenuation_radius", 2300.0)
        comp.set_editor_property("cast_shadows", False)
    except Exception as exc:
        unreal.log_warning("HARBOR interior light setup: " + str(exc))
    marker_pos = (x, y + direction * (depth / 2 + 320), 120)
    point = unreal.EditorLevelLibrary.spawn_actor_from_class(
        unreal.TargetPoint, v(*marker_pos))
    actor_label(point, "POI_" + tag_name)
    tag(point, ["HarborCity.POI", "HarborCity." + tag_name])
    return marker_pos


def place_city_block(name, cx, cy, district, landmark=None):
    if landmark:
        width, depth = (4400, 4400) if landmark != "Garage" else (6500, 5000)
        accent = "coral" if landmark in ("Police", "WeaponShop", "Garage") else "teal"
        accessible_interior(name + "_" + landmark, cx, cy, width, depth,
                            1150 if landmark != "Garage" else 900, accent, landmark)
        procedural_building(name + "_Annex", cx + 3600, cy + 1900,
                            1700, 2800, 1500, "graphite", "sand")
        return
    if district == "industrial":
        if not imported(name + "_Warehouse", "Warehouse",
                        (cx - 1800, cy + 500, 20), (4800, 4200, 1500)):
            procedural_building(name + "_Warehouse", cx - 1800, cy + 500,
                                4800, 4200, 1500, "graphite_light", "rust")
        for i in range(3):
            box(name + "_Container_{}".format(i),
                (cx + 2400, cy - 2400 + i * 1800, 175),
                (2100, 900, 350), ("coral", "teal", "sand")[i])
        return
    if district == "downtown":
        height = RNG.randrange(5500, 14500, 900)
        footprint = RNG.randrange(3300, 4400, 200)
        if not imported(name + "_Imported", "Building",
                        (cx - 2300, cy + 2300, 20),
                        (footprint, footprint, height), RNG.choice((0, 90, 180, 270))):
            procedural_building(name + "_Tower", cx - 2300, cy + 2300,
                                footprint, footprint, height, "graphite", "teal")
        procedural_building(name + "_Midrise", cx + 2350, cy - 2050,
                            3500, 3700, height * 0.58, "sand", "coral")
    else:
        if not imported(name + "_Imported", "Building",
                        (cx - 2200, cy + 2200, 20),
                        (3400, 3300, 2500), RNG.choice((0, 90, 180, 270))):
            procedural_building(name + "_Apartment", cx - 2200, cy + 2200,
                                3400, 3300, 2600, "sand", "teal")
        if not imported(name + "_House", "House", (cx + 2350, cy - 2000, 20),
                        (2900, 3100, 1400), RNG.choice((0, 90, 180, 270))):
            procedural_building(name + "_House", cx + 2350, cy - 2000,
                                2900, 3100, 1400, "cream", "coral")
        box(name + "_Yard", (cx + 2350, cy + 2000, 36),
            (3000, 2300, 70), "grass")


def build_park():
    cx, cy = -18000, 25000
    box("Park_Lawn", (cx, cy, 34), (10000, 8300, 68), "grass")
    for i, (sx, sy, h) in enumerate(((7200, 6400, 160), (5300, 4400, 240),
                                     (3400, 2600, 330))):
        box("Park_Terrace_{}".format(i), (cx - 500, cy + 400, h / 2 + 70),
            (sx, sy, h), "grass_light" if i % 2 else "grass")
    box("Park_Lookout", (cx - 500, cy + 400, 525),
        (2350, 1500, 90), "sand")
    # Small 30-45 cm rises make the viewpoint reachable by a character.
    steps = ((-4100, 100), (-3800, 140), (-3500, 180), (-3200, 220),
             (-2900, 230), (-2500, 260), (-2100, 290), (-1700, 310),
             (-1300, 340), (-900, 380), (-500, 400), (-150, 440),
             (150, 480), (450, 525), (750, 570))
    for i, (dy, top) in enumerate(steps):
        box("Park_Step_{:02d}".format(i),
            (cx - 500, cy + dy, top / 2), (450, 310, top), "sand")
    for i in range(13):
        x = cx - 4100 + (i % 5) * 2050
        y = cy - 3000 + (i // 5) * 2350
        if abs(x - (cx - 500)) < 1500 and abs(y - (cy + 400)) < 1500:
            continue
        if not imported("Park_Tree_{}".format(i), "Tree", (x, y, 70),
                        (500, 500, 900)):
            box("Park_Trunk_{}".format(i), (x, y, 270),
                (105, 105, 540), "rust")
            sphere("Park_Crown_{}".format(i), (x, y, 700),
                   (650, 650, 620), "grass_light", False)
    marker = unreal.EditorLevelLibrary.spawn_actor_from_class(
        unreal.TargetPoint, v(cx - 500, cy + 400, 650))
    actor_label(marker, "POI_ParkViewpoint")
    tag(marker, ["HarborCity.POI", "HarborCity.ParkViewpoint"])


def build_airstrip():
    cx, cy = 18000, 25000
    box("Airstrip_Apron", (cx, cy, 42), (9400, 7800, 82), "road_edge")
    box("Airstrip_EntryStep", (21000, 20970, 31),
        (1500, 280, 62), "sidewalk")
    box("Airstrip_Runway", (cx, cy + 500, 88), (8700, 1700, 12), "asphalt")
    for i, x in enumerate(range(14500, 22000, 1500)):
        box("Airstrip_Dash_{}".format(i), (x, cy + 500, 96),
            (700, 24, 3), "cream", False)
    box("Airstrip_Hangar", (cx - 1600, cy - 2500, 650),
        (3400, 1800, 1200), "graphite_light")
    box("Airstrip_HangarRoof", (cx - 1600, cy - 2500, 1275),
        (3600, 2000, 90), "teal")
    # A visual helipad at the coast, separate from the short ultralight strip.
    box("Helipad", (16000, -17800, 60), (2100, 1500, 80), "graphite")
    box("Helipad_HA", (16000, -17800, 104), (1000, 110, 4), "cream", False)
    box("Helipad_HB", (15700, -17800, 104), (100, 750, 4), "cream", False)
    box("Helipad_HC", (16300, -17800, 104), (100, 750, 4), "cream", False)
    for name, pos in (("Airstrip", (21000, 22000, 145)),
                      ("Helipad", (16000, -17800, 145))):
        marker = unreal.EditorLevelLibrary.spawn_actor_from_class(
            unreal.TargetPoint, v(*pos))
        actor_label(marker, "POI_" + name)
        tag(marker, ["HarborCity.POI", "HarborCity." + name])


def build_city_blocks():
    blocks = (
        (-18000, -10000, "industrial", "ServiceDepot"),
        (-6000, -10000, "industrial", "GasStation"),
        (6000, -10000, "industrial", None),
        (-18000, 2000, "residential", None),
        (-6000, 2000, "residential", None),
        (6000, 2000, "downtown", "Hospital"),
        (18000, 2000, "downtown", None),
        (6000, 14000, "downtown", "TechStore"),
        (18000, 14000, "downtown", "Police"),
        (-6000, 25000, "residential", None),
        (6000, 25000, "downtown", None),
    )
    for i, (x, y, district, landmark) in enumerate(blocks):
        place_city_block("Block_{:02d}".format(i), x, y, district, landmark)
    accessible_interior("Safehouse", -18000, 14520, 3400, 4400,
                        1150, "teal", "Safehouse")
    accessible_interior("WeaponShop", -5000, 11520, 4400, 4400,
                        1150, "coral", "WeaponShop")
    accessible_interior("ClothingShop", -9500, 14520, 2200, 4400,
                        1150, "teal", "ClothingShop")
    accessible_interior("Clinic", -21000, 17330, 2200, 2700,
                        900, "coral", "Clinic", front="north")
    accessible_interior("Garage", 16000, -6920, 6500, 3200,
                        900, "coral", "Garage", front="north")
    procedural_building("ServiceYardWarehouse", 21400, -11400,
                        3100, 4800, 1600, "graphite_light", "rust")
    build_park()
    build_airstrip()


def build_play_start_and_navigation():
    player = unreal.EditorLevelLibrary.spawn_actor_from_class(
        unreal.PlayerStart, v(1400, 6900, 160), rotation(40))
    actor_label(player, "PlayerStart")
    tag(player, ["HarborCity.PlayerStart"])
    try:
        nav = unreal.EditorLevelLibrary.spawn_actor_from_class(
            unreal.NavMeshBoundsVolume, v(0, 5500, 1800))
        actor_label(nav, "NavigationBounds")
        nav.set_actor_scale3d(v(300, 245, 20))
    except Exception as exc:
        unreal.log_warning("HARBOR: NavMeshBoundsVolume creation needs editor follow-up: " + str(exc))


def service_sign(label, x, y, title, accent):
    box(label + "_SignPost", (x + 650, y, 205),
        (45, 45, 410), "graphite")
    box(label + "_SignFace", (x + 650, y, 405),
        (1000, 70, 250), accent, False)
    try:
        text_actor = unreal.EditorLevelLibrary.spawn_actor_from_class(
            unreal.TextRenderActor, v(x + 280, y - 48, 400), rotation(0))
        actor_label(text_actor, label + "_Text")
        comp = text_actor.get_component_by_class(unreal.TextRenderComponent)
        comp.set_text(title)
        comp.set_editor_property("world_size", 145.0)
        comp.set_text_render_color(unreal.Color(255, 246, 218, 255))
    except Exception as exc:
        unreal.log_warning("HARBOR optional sign text {}: {}".format(label, exc))


def build_service_signs():
    signs = (
        ("Arms", -5000, 9000, "ARMS", "coral"),
        ("Garage", 16000, -5000, "GARAGE", "coral"),
        ("Clinic", -21000, 19000, "CLINIC", "teal"),
        ("Home", -18000, 12000, "HOME", "teal"),
        ("Marina", 0, -18000, "PIER", "teal"),
        ("Airfield", 21000, 22000, "AIRFIELD", "sand"),
    )
    for name, x, y, title, accent in signs:
        service_sign("Service_" + name, x, y, title, accent)


def build_lighting():
    sun = unreal.EditorLevelLibrary.spawn_actor_from_class(
        unreal.DirectionalLight, v(0, 0, 19000), rotation(-120, -45))
    actor_label(sun, "Sun")
    tag(sun, ["HarborCity.Sun"])
    try:
        comp = sun.get_editor_property("light_component")
        comp.set_editor_property("mobility", unreal.ComponentMobility.MOVABLE)
        comp.set_editor_property("intensity", 6.2)
        comp.set_editor_property("atmosphere_sun_light", True)
    except Exception as exc:
        unreal.log_warning("HARBOR: sun property setup: " + str(exc))
    sky = unreal.EditorLevelLibrary.spawn_actor_from_class(
        unreal.SkyLight, v(0, 0, 1000))
    actor_label(sky, "SkyLight")
    try:
        comp = sky.get_editor_property("light_component")
        comp.set_editor_property("mobility", unreal.ComponentMobility.MOVABLE)
        comp.set_editor_property("intensity", 1.1)
        comp.set_editor_property("real_time_capture", True)
    except Exception as exc:
        unreal.log_warning("HARBOR: sky light property setup: " + str(exc))
    try:
        atmosphere = unreal.EditorLevelLibrary.spawn_actor_from_class(
            unreal.SkyAtmosphere, v(0, 0, 0))
        actor_label(atmosphere, "SkyAtmosphere")
    except Exception as exc:
        unreal.log_warning("HARBOR: SkyAtmosphere unavailable: " + str(exc))
    try:
        fog = unreal.EditorLevelLibrary.spawn_actor_from_class(
            unreal.ExponentialHeightFog, v(0, 0, -100))
        actor_label(fog, "CoastalFog")
        comp = fog.get_editor_property("component")
        comp.set_editor_property("fog_density", 0.008)
    except Exception as exc:
        unreal.log_warning("HARBOR: fog setup: " + str(exc))


def main():
    unreal.log("HARBOR WORLD BUILD START " + LEVEL)
    load_materials()
    load_meshes()
    prepare_level()
    build_ground_and_water()
    build_roads()
    build_streetscape()
    build_city_blocks()
    build_service_signs()
    build_play_start_and_navigation()
    build_lighting()
    if not unreal.EditorLevelLibrary.save_current_level():
        raise RuntimeError("Could not save HarborCity level")
    unreal.EditorAssetLibrary.save_directory("/Game/GTA", only_if_is_dirty=True,
                                            recursive=True)
    unreal.log("HARBOR WORLD BUILD COMPLETE: {} actors, level {}".format(
        ACTOR_COUNT, LEVEL))


if __name__ == "__main__":
    main()
