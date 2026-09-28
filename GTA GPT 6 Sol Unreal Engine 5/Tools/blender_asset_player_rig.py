import bpy
from pathlib import Path

BLEND_PATH = Path(bpy.data.filepath)
if BLEND_PATH.name.casefold() != "UnrealGTAGPT6Sol.blend".casefold():
    raise RuntimeError("Wrong Blender project")

root=bpy.data.collections.get("GTA_Assets")
coll=bpy.data.collections.get("GTA_PlayerRig")
if coll is None:
    coll=bpy.data.collections.new("GTA_PlayerRig")
    root.children.link(coll)
for obj in list(coll.objects):
    bpy.data.objects.remove(obj,do_unlink=True)

src=bpy.data.objects["Player"]
skinned=src.copy()
skinned.data=src.data.copy()
skinned.name="PlayerSkinned"
coll.objects.link(skinned)
skinned.hide_set(False)

arm_data=bpy.data.armatures.new("PlayerSkeletonData")
rig=bpy.data.objects.new("PlayerSkeleton",arm_data)
coll.objects.link(rig)
bpy.ops.object.select_all(action="DESELECT")
rig.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.object.mode_set(mode="EDIT")
specs=[
    ("root",(0,0,0),(0,0,.13),None),
    ("pelvis",(0,0,.91),(0,0,1.12),"root"),
    ("spine",(0,0,1.12),(0,0,1.52),"pelvis"),
    ("neck",(0,0,1.52),(0,0,1.65),"spine"),
    ("head",(0,0,1.65),(0,0,1.96),"neck"),
]
for side,label in ((-1,"L"),(1,"R")):
    y=.15*side
    specs += [
        ("thigh_"+label,(0,y,.99),(.01,y,.56),"pelvis"),
        ("shin_"+label,(.01,y,.56),(.04,y,.15),"thigh_"+label),
        ("foot_"+label,(.04,y,.15),(.24,y,.075),"shin_"+label),
        ("upper_arm_"+label,(0,.30*side,1.49),(.06,.39*side,1.18),"spine"),
        ("lower_arm_"+label,(.06,.39*side,1.18),(.13,.36*side,.99),"upper_arm_"+label),
        ("hand_"+label,(.13,.36*side,.99),(.16,.36*side,.89),"lower_arm_"+label),
    ]
for name,head,tail,parent in specs:
    bone=arm_data.edit_bones.new(name)
    bone.head=head
    bone.tail=tail
    if parent:
        bone.parent=arm_data.edit_bones[parent]
bpy.ops.object.mode_set(mode="OBJECT")

groups={name:skinned.vertex_groups.new(name=name) for name,head,tail,parent in specs}
for vert in skinned.data.vertices:
    x,y,z=vert.co
    side="L" if y<0 else "R"
    if abs(y)>.28 and .86<z<1.65:
        name=("hand_" if z<1.01 else "lower_arm_" if z<1.23 else "upper_arm_")+side
    elif z<.17:
        name="foot_"+side
    elif z<.56:
        name="shin_"+side
    elif z<1.01:
        name="thigh_"+side
    elif z<1.14:
        name="pelvis"
    elif z<1.57:
        name="spine"
    elif z<1.66:
        name="neck"
    else:
        name="head"
    groups[name].add([vert.index],1.0,"REPLACE")

mod=skinned.modifiers.new("Gameplay skeleton","ARMATURE")
mod.object=rig
skinned.parent=rig
rig.show_in_front=True
src.hide_set(True)
rig.hide_set(False)
skinned.hide_set(False)
bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH.resolve()),compress=True)
print("RIGGED",len(specs),"bones",len(skinned.data.vertices),"weighted vertices")
