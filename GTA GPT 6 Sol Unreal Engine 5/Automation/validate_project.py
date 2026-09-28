"""Headless Unreal Editor validation for the playable Harbor City project.

Run with UnrealEditor-Cmd.exe <uproject> -run=pythonscript
    -script=Automation/validate_project.py -unattended -nop4

Writes Saved/Logs/HarborValidation.json and exits with an error if any required
content, class, or map actor is missing. A temporary WorldDirector spawn is
destroyed without saving the map. PIE is not available in commandlet mode.
"""

import json
from pathlib import Path
import unreal


ROOT = Path(unreal.Paths.project_dir())
LEVEL = "/Game/GTA/Maps/HarborCity"
ASSET_ROOT = "/Game/GTA/Generated"
MESH_NAMES = ("Car", "Player", "Pedestrian", "Police", "Pistol", "Rifle",
              "Shotgun", "Building", "House", "Warehouse", "Tree",
              "Streetlamp", "TrafficLight")
CLASS_NAMES = ("SolGameMode", "SolWorldDirector", "SolPlayerCharacter",
               "SolVehicle", "SolNPC", "SolHUD")
SOUND_NAMES = ("SFX_Gunshot", "SFX_EngineLoop", "SFX_Horn", "SFX_SirenLoop",
               "SFX_Footstep", "SFX_RainLoop", "SFX_CityAmbient")
ERRORS = []
REPORT = {"level": LEVEL, "mesh_assets": {}, "gameplay_classes": {},
          "sound_assets": {}, "actors": {}, "checks": {}}


def check(condition, key, detail):
    REPORT["checks"][key] = bool(condition)
    if condition:
        unreal.log("HARBOR VALIDATE PASS " + key)
    else:
        ERRORS.append(detail)
        unreal.log_error("HARBOR VALIDATE FAIL " + key + ": " + detail)


def validate_assets():
    source_dir = ROOT / "SourceAssets" / "BlenderExports"
    exported = {p.stem for p in source_dir.rglob("*")
                if p.is_file() and p.suffix.lower() in (".fbx", ".glb")}
    for name in sorted(set(MESH_NAMES) | (exported - {"PlayerRig"})):
        path = ASSET_ROOT + "/SM_" + name
        obj = unreal.EditorAssetLibrary.load_asset(path)
        valid = isinstance(obj, unreal.StaticMesh)
        REPORT["mesh_assets"][name] = path if valid else None
        check(valid, "mesh_" + name, "Missing StaticMesh " + path)
    rig_path = ASSET_ROOT + "/SK_PlayerRig"
    if "PlayerRig" in exported:
        rig = unreal.EditorAssetLibrary.load_asset(rig_path)
        valid = isinstance(rig, unreal.SkeletalMesh)
        REPORT["mesh_assets"]["PlayerRig"] = rig_path if valid else None
        check(valid, "skeletal_PlayerRig", "Missing SkeletalMesh " + rig_path)
    for name in SOUND_NAMES:
        path = "/Game/GTA/Audio/" + name
        sound = (unreal.EditorAssetLibrary.load_asset(path)
                 if unreal.EditorAssetLibrary.does_asset_exist(path) else None)
        valid = isinstance(sound, unreal.SoundWave)
        REPORT["sound_assets"][name] = path if valid else None
        check(valid, "sound_" + name, "Missing SoundWave " + path)
        if valid:
            expected_loop = name in ("SFX_EngineLoop", "SFX_RainLoop", "SFX_CityAmbient")
            check(bool(sound.get_editor_property("looping")) == expected_loop,
                  "sound_loop_" + name, "Wrong SoundWave loop flag for " + path)


def validate_classes():
    for name in CLASS_NAMES:
        path = "/Script/Unreal_GTA_GPT6Sol." + name
        try:
            cls = unreal.load_class(None, path)
        except Exception:
            cls = None
        REPORT["gameplay_classes"][name] = path if cls else None
        check(bool(cls), "class_" + name, "Missing compiled gameplay class " + path)


def validate_map():
    check(unreal.EditorAssetLibrary.does_asset_exist(LEVEL),
          "map_exists", "Missing map " + LEVEL)
    if not unreal.EditorAssetLibrary.does_asset_exist(LEVEL):
        return
    loaded = unreal.EditorLevelLibrary.load_level(LEVEL)
    check(bool(loaded), "map_load", "Could not load map " + LEVEL)
    if not loaded:
        return
    actors = list(unreal.EditorLevelLibrary.get_all_level_actors())
    counts = {
        "total": len(actors),
        "player_starts": sum(isinstance(a, unreal.PlayerStart) for a in actors),
        "directional_lights": sum(isinstance(a, unreal.DirectionalLight) for a in actors),
        "sky_lights": sum(isinstance(a, unreal.SkyLight) for a in actors),
        "navigation_bounds": sum(isinstance(a, unreal.NavMeshBoundsVolume) for a in actors),
        "static_mesh_actors": sum(isinstance(a, unreal.StaticMeshActor) for a in actors),
        "points_of_interest": sum("HC_POI_" in a.get_actor_label() for a in actors),
    }
    REPORT["actors"] = counts
    check(counts["total"] >= 250, "actor_density",
          "Expected at least 250 authored level actors, got {}".format(counts["total"]))
    for key in ("player_starts", "directional_lights", "sky_lights",
                "navigation_bounds"):
        check(counts[key] >= 1, key, "Map has no " + key)
    check(counts["points_of_interest"] >= 10, "service_locations",
          "Expected 10 or more tagged POI actors")
    player = next((a for a in actors if isinstance(a, unreal.PlayerStart)), None)
    if player:
        location = player.get_actor_location()
        heading = player.get_actor_rotation()
        start_ok = (abs(location.x - 1400) < 100 and
                    abs(location.y - 6900) < 100 and
                    abs(heading.pitch) < 1 and abs(heading.yaw - 40) < 1)
    else:
        start_ok = False
    check(start_ok, "safe_player_start", "PlayerStart is misplaced or pitched")
    sun = next((a for a in actors if isinstance(a, unreal.DirectionalLight)), None)
    try:
        sun_movable = (sun.get_editor_property("light_component").get_editor_property("mobility")
                       == unreal.ComponentMobility.MOVABLE)
    except Exception:
        sun_movable = False
    check(sun_movable, "movable_sun", "Sun must be Movable for time of day")
    ground = next((a for a in actors if a.get_actor_label() == "HC_Land_600m"), None)
    if ground:
        scale = ground.get_actor_scale3d()
        size_ok = abs(scale.x - 600.0) < 1.0 and abs(scale.y - 490.0) < 1.0
    else:
        size_ok = False
    check(size_ok, "map_600m_extent", "Land base missing or wrong scale")
    sea = next((a for a in actors if a.get_actor_label() == "HC_SeaSurface"), None)
    if sea:
        surface = sea.get_editor_property("static_mesh_component")
        sea_clear = (str(surface.get_collision_profile_name()) == "NoCollision" and
                     surface.get_collision_enabled() == unreal.CollisionEnabled.NO_COLLISION)
    else:
        sea_clear = False
    check(sea_clear, "sea_surface_no_collision",
          "Visual sea surface blocks boats or swimmers")


def validate_temporary_spawn():
    path = "/Script/Unreal_GTA_GPT6Sol.SolWorldDirector"
    try:
        cls = unreal.load_class(None, path)
        if not cls:
            return
        actor = unreal.EditorLevelLibrary.spawn_actor_from_class(
            cls, unreal.Vector(0, 0, 20000))
        spawned = actor is not None
        if actor:
            unreal.EditorLevelLibrary.destroy_actor(actor)
        check(spawned, "world_director_editor_spawn",
              "WorldDirector class loaded but could not spawn in Editor world")
    except Exception as exc:
        check(False, "world_director_editor_spawn", str(exc))


def main():
    unreal.log("HARBOR VALIDATION START")
    validate_assets()
    validate_classes()
    validate_map()
    if REPORT["checks"].get("map_load"):
        validate_temporary_spawn()
    REPORT["checks"]["pie_commandlet"] = "unavailable: use Editor PIE or -game smoke test"
    REPORT["errors"] = ERRORS
    output = ROOT / "Saved" / "Logs" / "HarborValidation.json"
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(REPORT, ensure_ascii=False, indent=2), encoding="utf-8")
    unreal.log("HARBOR VALIDATION RESULT: {} error(s); {}".format(len(ERRORS), output))
    if ERRORS:
        raise RuntimeError("Harbor City validation failed: " + "; ".join(ERRORS))


if __name__ == "__main__":
    main()
