coll = start_asset("CarWheelParts")
for x,position in ((1.43,"F"),(-1.48,"R")):
    for side,y in (("L",-.91),("R",.91)):
        tire=cyl(coll,"Tire",(x,y,.41),.39,.24,"rubber",16,(math.pi/2,0,0))
        rim=cyl(coll,"Rim",(x,y+(-.134 if y<0 else .134),.41),.23,.035,"silver",12,(math.pi/2,0,0))
        hub=cyl(coll,"Hub",(x,y+(-.158 if y<0 else .158),.41),.075,.04,"graphite",10,(math.pi/2,0,0))
        bpy.ops.object.select_all(action="DESELECT")
        for part in (tire,rim,hub):
            part.select_set(True)
        bpy.context.view_layer.objects.active=tire
        bpy.ops.object.join()
        wheel=bpy.context.object
        wheel.name="CarWheel_"+position+side
        bpy.context.scene.cursor.location=(x,y,.41)
        bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
        wheel.hide_set(True)
        print("PIVOT",wheel.name,tuple(round(v,3) for v in wheel.location))

coll = start_asset("HelicopterRotorParts")
hub=cyl(coll,"Rotor Hub",(-.16,0,2.77),.20,.13,"graphite",12)
blade_x=box(coll,"Rotor X",(-.16,0,2.78),(7.0,.20,.045),"graphite",.025)
blade_y=box(coll,"Rotor Y",(-.16,0,2.78),(.20,7.0,.045),"graphite",.025)
bpy.ops.object.select_all(action="DESELECT")
for part in (hub,blade_x,blade_y):
    part.select_set(True)
bpy.context.view_layer.objects.active=hub
bpy.ops.object.join()
main_rotor=bpy.context.object
main_rotor.name="HelicopterMainRotor"
bpy.context.scene.cursor.location=(-.16,0,2.77)
bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
main_rotor.hide_set(True)
print("PIVOT",main_rotor.name,tuple(round(v,3) for v in main_rotor.location))

tail_v=box(coll,"Tail V",(-4.17,-.22,2.11),(.07,.035,1.05),"graphite",.015)
tail_h=box(coll,"Tail H",(-4.17,-.22,2.11),(1.05,.035,.07),"graphite",.015)
bpy.ops.object.select_all(action="DESELECT")
tail_v.select_set(True)
tail_h.select_set(True)
bpy.context.view_layer.objects.active=tail_v
bpy.ops.object.join()
tail_rotor=bpy.context.object
tail_rotor.name="HelicopterTailRotor"
bpy.context.scene.cursor.location=(-4.17,-.22,2.11)
bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
tail_rotor.hide_set(True)
print("PIVOT",tail_rotor.name,tuple(round(v,3) for v in tail_rotor.location))

car=bpy.data.objects.get("Car")
if car:
    car.hide_set(False)
bpy.context.scene.cursor.location=(0,0,0)
save_master()
