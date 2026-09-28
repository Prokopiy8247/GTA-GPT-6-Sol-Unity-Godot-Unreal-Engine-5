coll = start_asset("Building")
box(coll,"Office mass",(0,0,7.2),(12,8,14.4),"sand",.08)
box(coll,"Dark plinth",(0,0,1.2),(12.15,8.15,2.4),"graphite",.05)
for floor in range(1,5):
    z = 2.45 + floor*2.45
    box(coll,"Floor belt",(0,0,z),(12.22,8.22,.12),"cream",.015)
    for y in (-3.0,-1.5,0,1.5,3.0):
        box(coll,"Front window frame",(6.085,y,z+1.15),(.11,1.12,1.55),"graphite",.025)
        box(coll,"Front blue glazing",(6.151,y,z+1.15),(.025,.95,1.36),"glass",.008)
        box(coll,"Rear window",(-6.085,y,z+1.15),(.10,1.02,1.42),"glass",.01)
    for x in (-4.3,-2.15,0,2.15,4.3):
        for side in (-1,1):
            box(coll,"Side window",(x,side*4.07,z+1.15),(1.15,.08,1.40),"glass",.01)
    for y in (-3.75,3.75):
        box(coll,"Facade pilaster",(6.16,y,z+1.16),(.17,.22,2.25),"steel",.02)
box(coll,"Main entrance frame",(6.18,0,1.2),(.22,2.65,2.2),"steel",.035)
box(coll,"Entrance glazing",(6.31,0,1.15),(.026,2.32,2.0),"glass_light",.005)
box(coll,"Entrance lintel",(6.34,0,2.38),(.2,3.2,.27),"coral",.025)
for y in (-2.8,2.8):
    box(coll,"Display glazing",(6.18,y,1.30),(.03,2.0,1.55),"glass",.008)
box(coll,"Roof",(0,0,14.53),(12.35,8.35,.22),"graphite",.03)
for side in (-1,1):
    box(coll,"Roof parapet",(0,side*4.1,14.84),(12.4,.24,.58),"sand",.02)
box(coll,"Roof parapet front",(6.1,0,14.84),(.24,8.25,.58),"sand",.02)
join_asset(coll,"Building")

coll = start_asset("Warehouse")
box(coll,"Warehouse shell",(0,0,3.5),(18,14,7),"steel",.08)
box(coll,"Roof slab",(0,0,7.10),(18.5,14.5,.24),"graphite",.03)
for x in (-7,-3.5,0,3.5,7):
    box(coll,"Roof rib",(x,0,7.28),(.12,14.2,.22),"turquoise_dark",.015)
box(coll,"Rollup bay frame",(9.1,-3.2,2.65),(.18,5.0,5.1),"graphite",.025)
box(coll,"Rollup shutter",(9.2,-3.2,2.65),(.02,4.55,4.68),"silver",.005)
for z in (0.7,1.25,1.8,2.35,2.9,3.45,4.0,4.55):
    box(coll,"Rollup seam",(9.225,-3.2,z),(.02,4.57,.025),"graphite",.004)
box(coll,"Personnel door",(9.2,3.8,1.20),(.035,1.4,2.35),"graphite",.005)
box(coll,"Exit sign",(9.24,3.8,2.57),(.03,1.5,.27),"coral",.008)
for y in (-5.5,-1.8,1.8,5.5):
    box(coll,"High window",(9.15,y,5.9),(.035,1.65,.68),"glass",.015)
for x in (-6.0,0,6.0):
    for side in (-1,1):
        box(coll,"Side window",(x,side*7.07,5.82),(2.55,.035,.62),"glass",.015)
box(coll,"Dock lip",(9.45,-3.2,.16),(.65,5.4,.32),"sand",.035)
join_asset(coll,"Warehouse")

coll = start_asset("House")
box(coll,"House first floor",(0,0,2.15),(9,7,4.3),"cream",.08)
box(coll,"Foundation",(0,0,.24),(9.3,7.3,.48),"sand",.035)
roof_verts=[(-4.8,-3.8,4.20),(4.8,-3.8,4.20),(-4.8,0,6.15),(4.8,0,6.15),(-4.8,3.8,4.20),(4.8,3.8,4.20)]
roof_faces=[(0,1,3,2),(2,3,5,4),(0,2,4),(1,5,3),(0,4,5,1)]
mesh(coll,"Pitched coral roof",roof_verts,roof_faces,"coral")
box(coll,"Front porch slab",(5.2,0,.24),(1.6,4.1,.27),"sand",.03)
box(coll,"Front door",(4.56,0,1.31),(.06,1.25,2.35),"turquoise_dark",.02)
cyl(coll,"Door knob",(4.62,-.42,1.22),.04,.045,"amber",10,(0,math.pi/2,0))
for side in (-1,1):
    box(coll,"Front sash",(4.56,side*2.25,1.77),(.06,1.25,1.42),"glass",.015)
    box(coll,"Front shutters",(4.63,side*3.02,1.77),(.045,.30,1.48),"turquoise_dark",.012)
    box(coll,"Porch post",(5.65,side*1.9,1.35),(.16,.16,2.6),"white",.01)
    for x in (-2.25,2.25):
        box(coll,"Side sash",(x,side*3.56,2.28),(1.18,.06,1.48),"glass",.015)
box(coll,"Porch awning",(5.15,0,2.78),(1.45,4.25,.17),"graphite",.018)
box(coll,"Chimney",(-2.7,-1.7,5.6),(.72,.72,2.35),"graphite",.02)
join_asset(coll,"House")

coll = start_asset("Streetlamp")
cyl(coll,"Anchor foot",(0,0,.09),.29,.18,"graphite",12)
cone(coll,"Tapered street pole",(0,0,2.50),.14,.085,4.86,"steel",10)
line(coll,"Swept arm",(0,0,4.81),(1.55,0,5.18),.065,"steel",10)
box(coll,"Lamp housing",(1.76,0,5.12),(.53,.45,.20),"graphite",.055)
box(coll,"White diffuser",(1.78,0,5.005),(.43,.36,.025),"cream",.012)
box(coll,"Service hatch",(0.13,-.03,1.35),(.027,.12,.22),"graphite",.01)
join_asset(coll,"Streetlamp")

coll = start_asset("TrafficLight")
cyl(coll,"Anchor",(0,0,.10),.29,.20,"graphite",10)
cone(coll,"Signal pole",(0,0,2.00),.13,.085,3.86,"steel",10)
line(coll,"Cantilever",(0,0,3.76),(1.0,0,3.76),.07,"steel",10)
box(coll,"Signal housing",(1.12,0,3.28),(.23,.43,1.18),"graphite",.045)
for z,key in ((3.65,"red"),(3.28,"amber"),(2.91,"green")):
    cyl(coll,"Signal lens",(1.26,0,z),.13,.06,key,12,(0,math.pi/2,0))
    cyl(coll,"Signal hood",(1.30,0,z+.105),.135,.055,"graphite",12,(0,math.pi/2,0))
box(coll,"Control box",(.15,0,1.48),(.30,.29,.39),"graphite",.025)
join_asset(coll,"TrafficLight")

coll = start_asset("Tree")
cyl(coll,"Trunk",(0,0,1.14),.22,2.28,"bark",8)
for z,r,h,key in ((2.45,1.25,2.10,"leaf"),(3.38,1.03,1.92,"leaf_light"),(4.16,.74,1.55,"leaf")):
    cone(coll,"Canopy",(0,0,z),r,.08,h,key,9)
for side in (-1,1):
    line(coll,"Low branch",(0,0,1.82),(.56,side*.52,2.56),.09,"bark",7)
join_asset(coll,"Tree")
save_master()
