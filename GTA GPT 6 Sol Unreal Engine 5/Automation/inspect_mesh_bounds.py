"""Log imported StaticMesh bounds for scale/attachment diagnostics."""

import json
from pathlib import Path
import unreal


root = Path(unreal.Paths.project_dir())
result = {}
for name in ("Player", "Pedestrian", "Police", "Car", "Boat", "Helicopter",
             "Plane", "Pistol", "Building", "House"):
    mesh = unreal.EditorAssetLibrary.load_asset("/Game/GTA/Generated/SM_" + name)
    if not mesh:
        continue
    bounds = mesh.get_bounds()
    origin = bounds.origin
    extent = bounds.box_extent
    result[name] = {
        "origin_cm": [origin.x, origin.y, origin.z],
        "half_extent_cm": [extent.x, extent.y, extent.z],
        "full_size_cm": [2 * extent.x, 2 * extent.y, 2 * extent.z],
    }
output = root / "Saved" / "Logs" / "MeshBounds.json"
output.write_text(json.dumps(result, indent=2), encoding="utf-8")
unreal.log("MESH BOUNDS " + str(output))
