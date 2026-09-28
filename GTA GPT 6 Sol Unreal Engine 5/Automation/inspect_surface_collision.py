"""Read-only map collision and vehicle-spawn clearance report."""

import json
from pathlib import Path
import unreal


unreal.EditorLevelLibrary.load_level("/Game/GTA/Maps/HarborCity")
actors = list(unreal.EditorLevelLibrary.get_all_level_actors())
targets = ("HC_SeaSurface", "HC_WaterFloor", "HC_Airstrip_Apron",
           "HC_Airstrip_Runway")


def bounds(actor):
    origin, extent = actor.get_actor_bounds(False)
    return {
        "origin": [origin.x, origin.y, origin.z],
        "extent": [extent.x, extent.y, extent.z],
        "min": [origin.x - extent.x, origin.y - extent.y, origin.z - extent.z],
        "max": [origin.x + extent.x, origin.y + extent.y, origin.z + extent.z],
    }


def collision(actor):
    component = actor.get_editor_property("static_mesh_component")
    result = {"enabled": str(component.get_collision_enabled()),
              "profile": str(component.get_collision_profile_name())}
    try:
        result["visibility_response"] = str(component.get_collision_response_to_channel(
            unreal.CollisionChannel.ECC_VISIBILITY))
    except Exception as exc:
        result["visibility_response_error"] = str(exc)
    return result


report = {"surfaces": {}, "spawn_clearance": {}}
for name in targets:
    actor = next((a for a in actors if a.get_actor_label() == name), None)
    if not actor:
        report["surfaces"][name] = None
        continue
    report["surfaces"][name] = {"bounds_cm": bounds(actor),
                                "collision": collision(actor)}

spawns = {
    "plane_previous": ([20000, 23000, 180], [340, 500, 100]),
    "plane_current_source": ([20000, 25500, 235], [340, 500, 100]),
    "boat": ([0, -22000, 120], [250, 105, 75]),
}
for name, (position, half_extent) in spawns.items():
    overlaps = []
    for actor in actors:
        if not isinstance(actor, unreal.StaticMeshActor):
            continue
        box = bounds(actor)
        origin, extent = box["origin"], box["extent"]
        if all(abs(position[i] - origin[i]) <= half_extent[i] + extent[i]
               for i in range(3)):
            overlaps.append({"actor": actor.get_actor_label(),
                             "bounds_cm": box,
                             "collision": collision(actor)})
    report["spawn_clearance"][name] = {
        "center_cm": position, "half_extent_cm": half_extent,
        "box_min_cm": [position[i] - half_extent[i] for i in range(3)],
        "box_max_cm": [position[i] + half_extent[i] for i in range(3)],
        "overlapping_static_mesh_actors": overlaps,
    }

output = Path(unreal.Paths.project_dir()) / "Saved" / "Logs" / "SurfaceCollision.json"
output.write_text(json.dumps(report, indent=2), encoding="utf-8")
