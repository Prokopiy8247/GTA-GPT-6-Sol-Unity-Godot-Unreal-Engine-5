"""Blender-side export recipe. Send this source to execute_blender_code on MCP port 9880.

The Blender add-on's safe mode does not permit reading scripts from Blender Python.
Use run_export_blender_assets_mcp.py to send this file through the MCP tool.
"""

import bpy


ASSETS = (
    "airplane",
    "bat",
    "bench",
    "boat",
    "car_compact",
    "car_police",
    "car_sedan",
    "car_sport",
    "car_suv",
    "firehydrant",
    "heavy_pistol",
    "helicopter",
    "house",
    "knife",
    "motorcycle",
    "npc_business",
    "npc_casual",
    "npc_civilian",
    "npc_police",
    "office",
    "pickup",
    "pistol",
    "player",
    "rifle",
    "shotgun",
    "smg",
    "sniper",
    "streetlamp",
    "trafficlight",
    "trash_bin",
    "tree",
    "van",
    "warehouse",
)

assert len(ASSETS) == 33
master = bpy.data.filepath.replace("\\", "/")
expected_suffix = "/godot-gta-gpt-6-sol/GodotGTAGPT6Sol.blend"
if not master.endswith(expected_suffix):
    raise RuntimeError("Wrong Blender instance or master file: " + bpy.data.filepath)

subset = bpy.context.scene.get("gta_export_subset", "")
names = tuple(part.strip() for part in subset.split(",") if part.strip()) if subset else ASSETS
unknown = tuple(name for name in names if name not in ASSETS)
if unknown:
    raise RuntimeError("Unknown GTA asset names: " + ", ".join(unknown))

for name in names:
    collection = bpy.data.collections.get("Asset_" + name)
    if collection is None:
        raise RuntimeError("Missing Blender collection: Asset_" + name)
    asset_root = collection.objects.get(name + "_root")
    if asset_root is None:
        raise RuntimeError("Missing asset root: " + name + "_root")
    rig = next((obj for obj in collection.objects if obj.type == "ARMATURE"), None)
    if rig is not None and (rig.animation_data is None or len(rig.animation_data.nla_tracks) < 2):
        raise RuntimeError("Animated asset needs idle and walk NLA tracks: " + name)

    original_location = asset_root.location.copy()
    try:
        asset_root.location = (0.0, 0.0, 0.0)
        bpy.context.view_layer.update()
        bpy.ops.object.select_all(action="DESELECT")
        for obj in collection.objects:
            obj.select_set(True)
        bpy.context.view_layer.objects.active = rig if rig is not None else asset_root
        filepath = bpy.path.abspath("//gta/generated/models/" + name + ".glb")
        bpy.ops.export_scene.gltf(
            filepath=filepath,
            export_format="GLB",
            use_selection=True,
            export_yup=True,
            export_apply=rig is None,
            export_animations=rig is not None,
            export_animation_mode="NLA_TRACKS" if rig is not None else "ACTIONS",
            export_skins=rig is not None,
        )
        print("EXPORTED", name, "rigged" if rig is not None else "static", filepath)
    finally:
        asset_root.location = original_location
        bpy.context.view_layer.update()

bpy.ops.object.select_all(action="DESELECT")
if "gta_export_subset" in bpy.context.scene:
    del bpy.context.scene["gta_export_subset"]
bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
print("GTA_MASTER_SAVED", bpy.data.filepath, "assets", len(names))
