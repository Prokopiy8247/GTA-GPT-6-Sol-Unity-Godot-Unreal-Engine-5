"""Read-only report of ordered material slots for garage paint configuration."""

import json
from pathlib import Path
import unreal


result = {}
for name in ("Car", "Sedan", "PoliceCar"):
    asset_path = "/Game/GTA/Generated/SM_" + name
    mesh = unreal.EditorAssetLibrary.load_asset(asset_path)
    if not isinstance(mesh, unreal.StaticMesh):
        raise RuntimeError("Missing StaticMesh: " + asset_path)
    entries = []
    for index, material in enumerate(mesh.get_editor_property("static_materials")):
        interface = material.get_editor_property("material_interface")
        entries.append({
            "index": index,
            "material_slot_name": str(material.get_editor_property("material_slot_name")),
            "imported_material_slot_name": str(
                material.get_editor_property("imported_material_slot_name")),
            "material_asset": interface.get_path_name() if interface else None,
        })
    result[name] = entries

output = Path(unreal.Paths.project_dir()) / "Saved" / "Logs" / "VehicleMaterialSlots.json"
output.write_text(json.dumps(result, indent=2), encoding="utf-8")
unreal.log("VEHICLE MATERIAL SLOTS " + str(output))
