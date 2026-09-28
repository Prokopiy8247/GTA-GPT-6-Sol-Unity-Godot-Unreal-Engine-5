import bpy
from pathlib import Path

BLEND_PATH = Path(bpy.data.filepath)
assert BLEND_PATH.name.casefold() == "UnrealGTAGPT6Sol.blend".casefold()
for obj in bpy.data.objects:
    if obj.name in {"Car","Player","Pedestrian","Police","Pistol","Building","Streetlamp","TrafficLight","Boat","Helicopter","Plane","Warehouse","House","Tree","Shotgun","Rifle","Sedan","PoliceCar","SUV","Pickup","Van","SportsCar","Motorcycle"}:
        obj.hide_set(obj.name != "PoliceCar")
target=bpy.data.objects["PoliceCar"]
for area in bpy.context.screen.areas:
    if area.type == "VIEW_3D":
        area.spaces.active.region_3d.view_location=(0,0,.9)
        area.spaces.active.region_3d.view_distance=8.5
print("VISIBLE", [(obj.name, obj.hide_get()) for obj in bpy.data.objects if obj.name in {"Van","PoliceCar"}])
