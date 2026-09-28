import bpy
import math
from pathlib import Path

BLEND_PATH = Path(bpy.data.filepath)
if BLEND_PATH.name.casefold() != "UnrealGTAGPT6Sol.blend".casefold():
    raise RuntimeError("Refusing to edit another Blender project: " + bpy.data.filepath)

COLORS = {
    "graphite": (0.055, 0.071, 0.085, 1),
    "dark": (0.018, 0.028, 0.037, 1),
    "steel": (0.24, 0.29, 0.31, 1),
    "silver": (0.56, 0.67, 0.69, 1),
    "turquoise": (0.018, 0.68, 0.65, 1),
    "turquoise_dark": (0.017, 0.32, 0.35, 1),
    "coral": (0.94, 0.28, 0.21, 1),
    "sand": (0.77, 0.66, 0.48, 1),
    "cream": (0.91, 0.85, 0.66, 1),
    "white": (0.92, 0.94, 0.88, 1),
    "glass": (0.09, 0.24, 0.31, 1),
    "glass_light": (0.31, 0.60, 0.68, 1),
    "rubber": (0.014, 0.018, 0.025, 1),
    "amber": (0.98, 0.55, 0.08, 1),
    "red": (0.88, 0.06, 0.07, 1),
    "green": (0.08, 0.80, 0.18, 1),
    "skin": (0.71, 0.43, 0.30, 1),
    "skin_light": (0.86, 0.61, 0.43, 1),
    "hair": (0.10, 0.055, 0.035, 1),
    "blue": (0.05, 0.17, 0.48, 1),
    "police": (0.025, 0.065, 0.16, 1),
    "leaf": (0.055, 0.38, 0.25, 1),
    "leaf_light": (0.18, 0.58, 0.32, 1),
    "bark": (0.30, 0.15, 0.09, 1),
}

def material(key):
    name = "GTA_" + key
    mat = bpy.data.materials.get(name)
    if mat is None:
        mat = bpy.data.materials.new(name)
    color = COLORS[key]
    mat.diffuse_color = color
    mat.use_nodes = True
    bsdf = next((node for node in mat.node_tree.nodes if node.type == "BSDF_PRINCIPLED"), None)
    if bsdf:
        socket = bsdf.inputs.get("Base Color")
        if socket:
            socket.default_value = color
        metallic = bsdf.inputs.get("Metallic")
        roughness = bsdf.inputs.get("Roughness")
        if metallic:
            metallic.default_value = 0.55 if key in {"steel", "silver"} else 0.0
        if roughness:
            roughness.default_value = 0.22 if key.startswith("glass") else 0.55
    return mat

def root_collection():
    root = bpy.data.collections.get("GTA_Assets")
    if root is None:
        root = bpy.data.collections.new("GTA_Assets")
        bpy.context.scene.collection.children.link(root)
    return root

def start_asset(name):
    root = root_collection()
    coll = bpy.data.collections.get("GTA_" + name)
    if coll is None:
        coll = bpy.data.collections.new("GTA_" + name)
        root.children.link(coll)
    for obj in list(coll.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    for old in root.children:
        for obj in old.objects:
            obj.hide_set(True)
    bpy.ops.object.select_all(action="DESELECT")
    return coll

def attach(obj, coll, key):
    for old_coll in tuple(obj.users_collection):
        old_coll.objects.unlink(obj)
    coll.objects.link(obj)
    obj.data.materials.clear()
    obj.data.materials.append(material(key))
    obj.hide_set(False)
    return obj

def box(coll, label, xyz, size, key, bevel=0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=xyz)
    obj = bpy.context.object
    obj.name = label
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        modifier = obj.modifiers.new("Soft edges", "BEVEL")
        modifier.width = bevel
        modifier.segments = 2
        bpy.ops.object.modifier_apply(modifier=modifier.name)
    return attach(obj, coll, key)

def cyl(coll, label, xyz, radius, depth, key, vertices=12, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=xyz, rotation=rotation)
    obj = bpy.context.object
    obj.name = label
    return attach(obj, coll, key)

def cone(coll, label, xyz, radius1, radius2, depth, key, vertices=8, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_cone_add(vertices=vertices, radius1=radius1, radius2=radius2, depth=depth, location=xyz, rotation=rotation)
    obj = bpy.context.object
    obj.name = label
    return attach(obj, coll, key)

def ico(coll, label, xyz, scale, key, subdivisions=1):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdivisions, radius=1, location=xyz)
    obj = bpy.context.object
    obj.name = label
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return attach(obj, coll, key)

def mesh(coll, label, verts, faces, key):
    data = bpy.data.meshes.new(label + "Mesh")
    data.from_pydata(verts, [], faces)
    data.update()
    obj = bpy.data.objects.new(label, data)
    coll.objects.link(obj)
    obj.data.materials.append(material(key))
    obj.hide_set(False)
    return obj

def line(coll, label, p0, p1, radius, key, vertices=8):
    from mathutils import Vector
    a, b = Vector(p0), Vector(p1)
    middle = (a + b) / 2
    direction = b - a
    obj = cyl(coll, label, middle, radius, direction.length, key, vertices)
    obj.rotation_euler = direction.to_track_quat("Z", "Y").to_euler()
    return obj

def join_asset(coll, name):
    objs = [obj for obj in coll.objects if obj.type == "MESH"]
    if not objs:
        raise RuntimeError("No meshes for " + name)
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objs:
        obj.hide_set(False)
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.join()
    joined = bpy.context.object
    joined.name = name
    bpy.context.scene.cursor.location = (0, 0, 0)
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
    joined.select_set(True)
    print("ASSET", name, "verts", len(joined.data.vertices), "dimensions", tuple(round(v, 3) for v in joined.dimensions))
    return joined

def save_master():
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH.resolve()), compress=True)
    print("SAVED", bpy.data.filepath)
