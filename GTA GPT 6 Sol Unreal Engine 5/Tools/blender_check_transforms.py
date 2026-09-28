import bpy
from mathutils import Vector

for name in ("Car", "Player", "Pistol", "Building", "Boat", "Helicopter", "Plane", "Sedan", "PoliceCar", "SUV", "Pickup", "Van", "SportsCar", "Motorcycle"):
    obj = bpy.data.objects.get(name)
    if obj:
        corners = [obj.matrix_world @ Vector(c) for c in obj.bound_box]
        dims = tuple(round(max(c[i] for c in corners)-min(c[i] for c in corners),3) for i in range(3))
        print(name, "ROT",tuple(round(v,3) for v in obj.rotation_euler), "SCALE",tuple(round(v,3) for v in obj.scale), "WORLD_DIMS",dims)
