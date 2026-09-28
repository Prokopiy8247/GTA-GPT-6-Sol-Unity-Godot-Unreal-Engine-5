import bpy
from pathlib import Path

BLEND_PATH = Path(bpy.data.filepath)
if BLEND_PATH.name.casefold() != "UnrealGTAGPT6Sol.blend".casefold():
    raise RuntimeError("Wrong Blender project")
for obj in bpy.data.objects:
    if any(coll.name.startswith("GTA_") for coll in obj.users_collection):
        obj.hide_set(obj.name != "Car")
for area in bpy.context.screen.areas:
    if area.type == "VIEW_3D":
        area.spaces.active.region_3d.view_location = (0,0,.85)
        area.spaces.active.region_3d.view_distance = 7.5
bpy.context.scene.cursor.location=(0,0,0)
bpy.ops.object.select_all(action="DESELECT")
car=bpy.data.objects["Car"]
car.select_set(True)
bpy.context.view_layer.objects.active=car
bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH.resolve()),compress=True)
print("FINAL", bpy.data.filepath, len(bpy.data.objects), len(bpy.data.materials))
