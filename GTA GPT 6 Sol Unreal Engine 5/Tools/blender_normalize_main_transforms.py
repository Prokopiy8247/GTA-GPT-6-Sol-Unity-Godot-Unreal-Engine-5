import bpy
from pathlib import Path

BLEND_PATH = Path(bpy.data.filepath)
if BLEND_PATH.name.casefold() != "UnrealGTAGPT6Sol.blend".casefold():
    raise RuntimeError("Wrong Blender project")

names = ("Car","Player","Pedestrian","Police","Pistol","Rifle","Shotgun","Building","Warehouse","House","Streetlamp","TrafficLight","Tree","Boat","Helicopter","Plane","Sedan","PoliceCar","SUV","Pickup","Van","SportsCar","Motorcycle")
for name in names:
    obj=bpy.data.objects.get(name)
    if obj is None:
        continue
    obj.hide_set(False)
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active=obj
    bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
    bpy.context.scene.cursor.location=(0,0,0)
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
    obj.hide_set(name!="Car")
    print("NORMALIZED",name,tuple(round(v,3) for v in obj.rotation_euler))
bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH.resolve()),compress=True)
