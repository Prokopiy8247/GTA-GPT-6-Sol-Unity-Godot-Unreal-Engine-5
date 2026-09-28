coll=start_asset("Bench")
for y in (-.65,.65):
    box(coll,"Bench leg front",(.23,y,.24),(.09,.08,.47),"graphite",.012)
    box(coll,"Bench leg rear",(-.24,y,.24),(.09,.08,.47),"graphite",.012)
    line(coll,"Bench frame",(-.24,y,.46),(.23,y,.46),.035,"steel",8)
    line(coll,"Back support",(-.24,y,.43),(-.38,y,.94),.035,"steel",8)
for x in (-.15,-.02,.11,.24):
    box(coll,"Seat plank",(x,0,.50),(.105,1.65,.07),"sand",.013)
for z in (.61,.75,.89):
    box(coll,"Backrest plank",(-.32,0,z),(.075,1.65,.095),"bark",.011)
join_asset(coll,"Bench")

coll=start_asset("Container")
box(coll,"Container core",(0,0,1.33),(6.1,2.42,2.66),"turquoise_dark",.025)
for x in (-3.06,3.06):
    for y in (-1.22,1.22):
        box(coll,"Corner post",(x,y,1.35),(.15,.15,2.70),"graphite",.012)
for side in (-1,1):
    for x in (-2.85,-2.45,-2.05,-1.65,-1.25,-.85,-.45,-.05,.35,.75,1.15,1.55,1.95,2.35,2.75):
        box(coll,"Ribbed side panel",(x,side*1.235,1.38),(.07,.052,2.43),"turquoise",.008)
    box(coll,"Warning stripe",(0,side*1.267,.39),(5.62,.018,.055),"coral",.008)
for y in (-.58,.58):
    box(coll,"Cargo door",(3.14,y,1.36),(.035,1.12,2.42),"steel",.008)
    line(coll,"Door locking bar",(3.185,y, .25),(3.185,y,2.46),.026,"graphite",8)
    box(coll,"Door latch",(3.23,y,1.20),(.035,.25,.09),"graphite",.01)
box(coll,"Roof corner trim",(0,0,2.69),(6.23,2.51,.10),"graphite",.015)
join_asset(coll,"Container")

coll=start_asset("Rock")
rock=ico(coll,"Angular boulder",(0,0,.56),(1.0,.76,.69),"steel",1)
for vert in rock.data.vertices:
    x,y,z=vert.co
    vert.co.x=x*(1.0+.11*math.sin(7*y+3*z))
    vert.co.y=y*(1.0+.09*math.sin(5*x+2*z))
    vert.co.z=z*(1.0+.10*math.sin(3*x+8*y))
ico(coll,"Secondary stone",(-.61,.38,.20),(.38,.32,.26),"graphite",1)
join_asset(coll,"Rock")

coll=start_asset("Barrier")
box(coll,"Concrete jersey barrier",(0,0,.52),(2.60,.47,1.04),"sand",.09)
box(coll,"Wide foot",(0,0,.13),(2.68,.77,.25),"steel",.04)
for x in (-.75,0,.75):
    box(coll,"Reflector stripe",(x,-.247,.77),(.32,.025,.26),"coral",.01)
    box(coll,"Reflector stripe",(x,.247,.77),(.32,.025,.26),"coral",.01)
join_asset(coll,"Barrier")

coll=start_asset("RoadCone")
box(coll,"Weighted foot",(0,0,.055),(.56,.56,.11),"graphite",.025)
cone(coll,"Orange cone",(0,0,.45),.23,.03,.78,"coral",10)
cyl(coll,"Reflective white ring",(0,0,.42),.147,.08,"white",10)
cyl(coll,"Black apex",(0,0,.83),.044,.06,"graphite",10)
join_asset(coll,"RoadCone")

coll=start_asset("Hydrant")
cyl(coll,"Hydrant footing",(0,0,.11),.27,.22,"graphite",12)
cyl(coll,"Hydrant barrel",(0,0,.47),.17,.70,"coral",12)
cyl(coll,"Crown",(0,0,.87),.21,.15,"coral",12)
for side in (-1,1):
    cyl(coll,"Side nozzle",(0,side*.22,.53),.10,.22,"steel",10,(math.pi/2,0,0))
    cyl(coll,"Nozzle cap",(0,side*.35,.53),.12,.055,"graphite",10,(math.pi/2,0,0))
cyl(coll,"Top nut",(0,0,.98),.055,.06,"steel",8)
join_asset(coll,"Hydrant")

coll=start_asset("Bin")
cone(coll,"Street waste bin",(0,0,.49),.30,.26,.91,"graphite",12)
cyl(coll,"Bin rim",(0,0,.96),.31,.075,"steel",12)
cyl(coll,"Bin lid",(0,0,1.00),.33,.07,"graphite",12)
box(coll,"Litter label",(.28,0,.61),(.018,.33,.20),"turquoise",.012)
join_asset(coll,"Bin")
save_master()
