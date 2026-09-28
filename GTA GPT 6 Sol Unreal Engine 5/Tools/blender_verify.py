import bpy

print("FILEPATH", bpy.data.filepath)
print("VERSION", bpy.app.version_string)
print("OBJECTS", [(obj.name, obj.type) for obj in bpy.data.objects])
print("FBX", hasattr(bpy.ops.export_scene, "fbx"))
print("GLTF", hasattr(bpy.ops.export_scene, "gltf"))
