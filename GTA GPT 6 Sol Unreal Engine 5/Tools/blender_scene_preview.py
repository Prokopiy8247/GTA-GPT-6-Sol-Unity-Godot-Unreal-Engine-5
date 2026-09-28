import bpy
from pathlib import Path

BLEND_PATH = Path(bpy.data.filepath)
if BLEND_PATH.name.casefold() != "UnrealGTAGPT6Sol.blend".casefold():
    raise RuntimeError("Wrong Blender project")
for name in ("Cube", "Camera", "Light"):
    obj = bpy.data.objects.get(name)
    if obj and not any(coll.name.startswith("GTA_") for coll in obj.users_collection):
        bpy.data.objects.remove(obj, do_unlink=True)
for area in bpy.context.screen.areas:
    if area.type == "VIEW_3D":
        space = area.spaces.active
        space.region_3d.view_location = (0, 0, 0.8)
        space.region_3d.view_distance = 7.5
        space.shading.color_type = "MATERIAL"
        space.overlay.show_floor = False
        space.overlay.show_axis_x = False
        space.overlay.show_axis_y = False
bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH.resolve()), compress=True)
print("CLEANED_SCENE", len(bpy.data.objects))
